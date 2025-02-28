// https://github.com/jchristn/SuperSimpleTcp

using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TapExtensions.Shared.Telnet
{
    public class TelnetClient : IDisposable
    {
        #region Public-Members

        public bool IsConnected { get; private set; }

        public TelnetClientSettings Settings
        {
            get => _settings;
            set => _settings = value ?? new TelnetClientSettings();
        }

        public TelnetClientEvents Events
        {
            get => _events;
            set => _events = value ?? new TelnetClientEvents();
        }

        /// <summary> Method to invoke to send a log message. </summary>
        public Action<string> Logger = null;

        /// <summary> The IpAddress:TcpPort of the server to which this client is connected to. </summary>
        public string ServerAddress => $"{_ipAddress}:{_tcpPort}";

        #endregion

        #region Private-Members

        private const string Header = "[Telnet] ";
        private TelnetClientSettings _settings = new TelnetClientSettings();
        private TelnetClientEvents _events = new TelnetClientEvents();
        private readonly string _ipAddress;
        private readonly int _tcpPort;
        private TcpClient _tcpClient;
        private NetworkStream _tcpStream;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private Task _dataReceiver;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private CancellationToken _token;
        private DateTime _lastActivity = DateTime.Now;
        private bool _isTimeout;

        #endregion

        #region Constructors

        /// <summary>
        ///     Instantiates the TCP client.
        ///     Set the Connected, Disconnected, and DataReceived callbacks. Once set, use Connect() to connect to the server.
        /// </summary>
        /// <param name="ipAddress">The IP Address of server.</param>
        /// <param name="tcpPort">The TCP Port on which to connect.</param>
        public TelnetClient(string ipAddress, int tcpPort)
        {
            if (string.IsNullOrEmpty(ipAddress)) throw new ArgumentNullException(nameof(ipAddress));
            if (tcpPort < 0) throw new ArgumentException("Port must be zero or greater.");

            _ipAddress = ipAddress;
            _tcpPort = tcpPort;
        }

        #endregion

        #region Public-Methods

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Connect()
        {
            if (IsConnected)
                return;

            Logger?.Invoke($"{Header}Connecting to {ServerAddress}");
            _tcpClient = _settings.LocalEndpoint == null ? new TcpClient() : new TcpClient(_settings.LocalEndpoint);
            _tcpClient.NoDelay = _settings.NoDelay;

            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
            _token.Register(() =>
            {
                if (_tcpStream == null) return;
                _tcpStream.Close();
            });

            var ar = _tcpClient.BeginConnect(_ipAddress, _tcpPort, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(_settings.ConnectTimeoutMs), false))
            {
                _tcpClient.Close();
                throw new TimeoutException($"Timeout connecting to {ServerAddress}");
            }

            _tcpClient.EndConnect(ar);
            _tcpStream = _tcpClient.GetStream();
            _tcpStream.ReadTimeout = _settings.ReadTimeoutMs;

            IsConnected = true;
            _lastActivity = DateTime.Now;
            _isTimeout = false;
            _events.HandleConnected(this, new ConnectionEventArgs(ServerAddress));
            _dataReceiver = Task.Run(() => DataReceiver(_token), _token);
            Task.Run(IdleServerMonitor, _token);
            Task.Run(ConnectedMonitor, _token);
        }

        public void Disconnect()
        {
            if (!IsConnected)
                return;

            Logger?.Invoke($"{Header}Disconnecting from {ServerAddress}");

            _tokenSource.Cancel();
            WaitCompletion();
            _tcpClient?.Close();
            IsConnected = false;
        }

        public async Task DisconnectAsync()
        {
            if (!IsConnected)
                return;

            Logger?.Invoke($"{Header}Disconnecting from {ServerAddress}");

            _tokenSource.Cancel();
            await WaitCompletionAsync();
            _tcpClient?.Close();
            IsConnected = false;
        }

        /// <summary> Send data to the server. </summary>
        /// <param name="data"> String containing data to send. </param>
        public void Send(string data)
        {
            if (string.IsNullOrEmpty(data)) throw new ArgumentNullException(nameof(data));
            if (!IsConnected) throw new IOException("Not connected to the server; use Connect() first.");

            var bytes = Encoding.UTF8.GetBytes(data);
            Send(bytes);
        }

        /// <summary> Send data to the server. </summary>
        /// <param name="data"> Byte array containing data to send. </param>
        public void Send(byte[] data)
        {
            if (data == null || data.Length < 1) throw new ArgumentNullException(nameof(data));
            if (!IsConnected) throw new IOException("Not connected to the server; use Connect() first.");

            using (var ms = new MemoryStream())
            {
                ms.Write(data, 0, data.Length);
                ms.Seek(0, SeekOrigin.Begin);
                SendInternal(data.Length, ms);
            }
        }

        /// <summary> Send data to the server. </summary>
        /// <param name="contentLength"> The number of bytes to read from the source stream to send. </param>
        /// <param name="stream"> Stream containing the data to send. </param>
        public void Send(long contentLength, Stream stream)
        {
            if (contentLength < 1) return;
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead) throw new InvalidOperationException("Cannot read from supplied stream.");
            if (!IsConnected) throw new IOException("Not connected to the server; use Connect() first.");

            SendInternal(contentLength, stream);
        }

        /// <summary> Send data to the server asynchronously. </summary>
        /// <param name="data">String containing data to send.</param>
        /// <param name="token">Cancellation token for canceling the request.</param>
        public async Task SendAsync(string data, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(data)) throw new ArgumentNullException(nameof(data));
            if (!IsConnected) throw new IOException("Not connected to the server; use Connect() first.");
            if (token == default) token = _token;

            var bytes = Encoding.UTF8.GetBytes(data);

            using (var ms = new MemoryStream())
            {
                await ms.WriteAsync(bytes, 0, bytes.Length, token).ConfigureAwait(false);
                ms.Seek(0, SeekOrigin.Begin);
                await SendInternalAsync(bytes.Length, ms, token).ConfigureAwait(false);
            }
        }

        /// <summary> Send data to the server asynchronously. </summary>
        /// <param name="data">Byte array containing data to send.</param>
        /// <param name="token">Cancellation token for canceling the request.</param>
        public async Task SendAsync(byte[] data, CancellationToken token = default)
        {
            if (data == null || data.Length < 1) throw new ArgumentNullException(nameof(data));
            if (!IsConnected) throw new IOException("Not connected to the server; use Connect() first.");
            if (token == default) token = _token;

            using (var ms = new MemoryStream())
            {
                await ms.WriteAsync(data, 0, data.Length, token).ConfigureAwait(false);
                ms.Seek(0, SeekOrigin.Begin);
                await SendInternalAsync(data.Length, ms, token).ConfigureAwait(false);
            }
        }

        /// <summary> Send data to the server asynchronously. </summary>
        /// <param name="contentLength">The number of bytes to read from the source stream to send.</param>
        /// <param name="stream">Stream containing the data to send.</param>
        /// <param name="token">Cancellation token for canceling the request.</param>
        public async Task SendAsync(long contentLength, Stream stream, CancellationToken token = default)
        {
            if (contentLength < 1) return;
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead) throw new InvalidOperationException("Cannot read from supplied stream.");
            if (!IsConnected) throw new IOException("Not connected to the server; use Connect() first.");
            if (token == default) token = _token;

            await SendInternalAsync(contentLength, stream, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        /// <summary> Dispose of the TCP client. </summary>
        /// <param name="disposing">Dispose of resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                IsConnected = false;

                if (_tokenSource != null)
                    if (!_tokenSource.IsCancellationRequested)
                    {
                        _tokenSource.Cancel();
                        _tokenSource.Dispose();
                    }

                if (_tcpStream != null)
                {
                    _tcpStream.Close();
                    _tcpStream.Dispose();
                }

                if (_tcpClient != null)
                {
                    _tcpClient.Close();
                    _tcpClient.Dispose();
                }

                Logger?.Invoke($"{Header}dispose complete");
            }
        }

        private async Task DataReceiver(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _tcpClient != null && _tcpClient.Connected)
                try
                {
                    await DataReadAsync(token).ContinueWith(async task =>
                    {
                        if (task.IsCanceled) return default;
                        var data = task.Result;

                        if (data != null)
                        {
                            _lastActivity = DateTime.Now;

                            Action action = () =>
                                _events.HandleDataReceived(this, new DataReceivedEventArgs(ServerAddress, data));
                            if (_settings.UseAsyncDataReceivedEvents)
                                _ = Task.Run(action, token);
                            else
                                action.Invoke();

                            return data;
                        }

                        await Task.Delay(100).ConfigureAwait(false);
                        return default;
                    }, token).ContinueWith(task => { }).ConfigureAwait(false);
                }
                catch (AggregateException)
                {
                    Logger?.Invoke($"{Header}data receiver canceled, disconnected");
                    break;
                }
                catch (IOException)
                {
                    Logger?.Invoke($"{Header}data receiver canceled, disconnected");
                    break;
                }
                catch (SocketException)
                {
                    Logger?.Invoke($"{Header}data receiver canceled, disconnected");
                    break;
                }
                catch (TaskCanceledException)
                {
                    Logger?.Invoke($"{Header}data receiver task canceled, disconnected");
                    break;
                }
                catch (OperationCanceledException)
                {
                    Logger?.Invoke($"{Header}data receiver operation canceled, disconnected");
                    break;
                }
                catch (ObjectDisposedException)
                {
                    Logger?.Invoke($"{Header}data receiver canceled due to disposal, disconnected");
                    break;
                }
                catch (Exception e)
                {
                    Logger?.Invoke($"{Header}data receiver exception:{Environment.NewLine}{e}{Environment.NewLine}");
                    break;
                }

            Logger?.Invoke($"{Header}disconnection detected");

            IsConnected = false;

            if (!_isTimeout)
                _events.HandleDisconnected(this, new ConnectionEventArgs(ServerAddress, DisconnectReason.Normal));
            else _events.HandleDisconnected(this, new ConnectionEventArgs(ServerAddress, DisconnectReason.Timeout));

            Dispose();
        }

        private async Task<ArraySegment<byte>> DataReadAsync(CancellationToken token)
        {
            var buffer = new byte[_settings.StreamBufferSize];

            try
            {
                var read = await _tcpStream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);

                if (read > 0)
                    using (var ms = new MemoryStream())
                    {
                        ms.Write(buffer, 0, read);
                        return new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Length);
                    }

                var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
                var tcpConnections = ipProperties.GetActiveTcpConnections()
                    .Where(x => x.LocalEndPoint.Equals(_tcpClient.Client.LocalEndPoint) &&
                                x.RemoteEndPoint.Equals(_tcpClient.Client.RemoteEndPoint)).ToArray();

                var isOk = false;

                if (tcpConnections != null && tcpConnections.Length > 0)
                {
                    var stateOfConnection = tcpConnections.First().State;
                    if (stateOfConnection == TcpState.Established) isOk = true;
                }

                if (!isOk) await DisconnectAsync();

                throw new SocketException();
            }
            catch (IOException)
            {
                // thrown if ReadTimeout (ms) is exceeded
                // see https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.networkstream.readtimeout?view=net-6.0
                // and https://github.com/dotnet/runtime/issues/24093
                return default;
            }
        }

        private void SendInternal(long contentLength, Stream stream)
        {
            var bytesRemaining = contentLength;
            var buffer = new byte[_settings.StreamBufferSize];

            try
            {
                _sendLock.Wait();

                while (bytesRemaining > 0)
                {
                    var bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        _tcpStream.Write(buffer, 0, bytesRead);
                        bytesRemaining -= bytesRead;
                    }
                }

                _tcpStream.Flush();
                _events.HandleDataSent(this, new DataSentEventArgs(ServerAddress, contentLength));
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task SendInternalAsync(long contentLength, Stream stream, CancellationToken token)
        {
            try
            {
                var bytesRemaining = contentLength;
                var buffer = new byte[_settings.StreamBufferSize];

                await _sendLock.WaitAsync(token).ConfigureAwait(false);

                while (bytesRemaining > 0)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                    if (bytesRead > 0)
                    {
                        await _tcpStream.WriteAsync(buffer, 0, bytesRead, token).ConfigureAwait(false);
                        bytesRemaining -= bytesRead;
                    }
                }

                await _tcpStream.FlushAsync(token).ConfigureAwait(false);
                _events.HandleDataSent(this, new DataSentEventArgs(ServerAddress, contentLength));
            }
            catch (TaskCanceledException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private void WaitCompletion()
        {
            try
            {
                _dataReceiver.Wait();
            }
            catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
            {
                Logger?.Invoke("Awaiting a canceled task");
            }
        }

        private async Task WaitCompletionAsync()
        {
            try
            {
                await _dataReceiver;
            }
            catch (TaskCanceledException)
            {
                Logger?.Invoke("Awaiting a canceled task");
            }
        }

        private async Task IdleServerMonitor()
        {
            while (!_token.IsCancellationRequested)
            {
                await Task.Delay(_settings.IdleServerEvaluationIntervalMs, _token).ConfigureAwait(false);

                if (_settings.IdleServerTimeoutMs == 0) continue;

                var timeoutTime = _lastActivity.AddMilliseconds(_settings.IdleServerTimeoutMs);

                if (DateTime.Now > timeoutTime)
                {
                    Logger?.Invoke($"{Header}disconnecting from {ServerAddress} due to timeout");
                    IsConnected = false;
                    _isTimeout = true;
                    _tokenSource.Cancel(); // DataReceiver will fire events including dispose
                }
            }
        }

        private async Task ConnectedMonitor()
        {
            while (!_token.IsCancellationRequested)
            {
                await Task.Delay(_settings.ConnectionLostEvaluationIntervalMs, _token).ConfigureAwait(false);

                if (!IsConnected)
                    continue; //Just monitor connected clients

                if (!PollSocket())
                {
                    Logger?.Invoke($"{Header}disconnecting from {ServerAddress} due to connection lost");
                    IsConnected = false;
                    _tokenSource.Cancel(); // DataReceiver will fire events including dispose
                }
            }
        }

        private bool PollSocket()
        {
            try
            {
                if (_tcpClient.Client == null || !_tcpClient.Client.Connected)
                    return false;

                /* pear to the documentation on Poll:
                 * When passing SelectMode.SelectRead as a parameter to the Poll method it will return
                 * -either- true if Socket.Listen(Int32) has been called and a connection is pending;
                 * -or- true if data is available for reading;
                 * -or- true if the connection has been closed, reset, or terminated;
                 * otherwise, returns false
                 */
                if (!_tcpClient.Client.Poll(0, SelectMode.SelectRead))
                    return true;

                var buff = new byte[1];
                var clientSentData = _tcpClient.Client.Receive(buff, SocketFlags.Peek) != 0;
                return clientSentData; //False here though Poll() succeeded means we had a disconnect!
            }
            catch (SocketException ex)
            {
                Logger?.Invoke($"{Header}poll socket from {ServerAddress} failed with ex = {ex}");
                return ex.SocketErrorCode == SocketError.TimedOut;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}