using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using OpenTap;
using Renci.SshNet;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Duts.Ssh
{
    [Display("DutControlSsh",
        Groups: new[] { "TapExtensions", "Duts", "Ssh" })]
    public class DutControlSsh : Dut, ISecureShell
    {
        #region Settings

        [Display("IP Address", Order: 1, Group: "SSH Settings")]
        public string IpAddress { get; set; }

        [Display("Port", Order: 2, Group: "SSH Settings", Description: "TCP port number (default value is 22)")]
        public int TcpPort { get; set; }

        [Display("Username", Order: 3, Group: "SSH Settings")]
        public string Username { get; set; }

        [Display("Password", Order: 4, Group: "SSH Settings")]
        public string Password { get; set; }

        [Display("Keep Alive Interval", Order: 5, Group: "SSH Settings")]
        [Unit("s")]
        public int KeepAliveInterval { get; set; }

        [Display("Verbose Logging", Order: 6, Group: "Debug", Collapsed: true,
            Description: "Enables verbose logging of SSH communication.")]
        public bool VerboseLoggingEnabled { get; set; } = true;

        #endregion

        private SshClient _sshClient;
        private ScpClient _scpClient;
        protected readonly object SshLock = new object();
        internal HashSet<string> InitializedFrmonHashSet;
        protected TimeSpan SshKeepAlive;

        public DutControlSsh()
        {
            // Default values
            Name = nameof(DutControlSsh);
            IpAddress = "127.0.0.1";
            TcpPort = 22;
            Username = "root";
            Password = "";
            KeepAliveInterval = 30;

            // Validation rules
            Rules.Add(() => IPAddress.TryParse(IpAddress, out _),
                "Not a valid IPv4 Address", nameof(IpAddress));
            Rules.Add(() => KeepAliveInterval >= 0,
                "Must be greater than or equal to zero", nameof(KeepAliveInterval));

            // Other
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            InitializedFrmonHashSet = new HashSet<string>();
            SshKeepAlive = new TimeSpan(0, 0, KeepAliveInterval);
        }

        public override void Open()
        {
            base.Open();
            IsConnected = false;
        }

        public override void Close()
        {
            Disconnect();
            base.Close();
            IsConnected = false;
        }

        public virtual void Connect()
        {
            ConnectSsh(5);
            ConnectScp(5);
            IsConnected = true;
        }

        private ConnectionInfo GetPasswordConnectionInfo()
        {
            var authenticationMethods = new List<AuthenticationMethod>
            {
                new PasswordAuthenticationMethod(Username, Password)
            };

            var passwordConnectionInfo = new ConnectionInfo(IpAddress, Username, authenticationMethods.ToArray());
            return passwordConnectionInfo;
        }

        internal virtual void ConnectSsh(long timeout)
        {
            ConnectSsh(timeout, GetPasswordConnectionInfo());
        }

        internal virtual void ConnectSsh(long timeout, ConnectionInfo connectionInfo)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (_sshClient == null)
                _sshClient = new SshClient(connectionInfo)
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(KeepAliveInterval)
                };

            while (!_sshClient.IsConnected && stopwatch.ElapsedMilliseconds < timeout * 1000)
            {
                try
                {
                    if (VerboseLoggingEnabled)
                        Log.Debug($"Connecting SSH to {IpAddress} on port {TcpPort}");

                    _sshClient.Connect();
                }
                catch (SocketException ex)
                {
                    Log.Debug(ex.ToString());
                }

                if (!_sshClient.IsConnected)
                    TapThread.Sleep(5000);
            }

            stopwatch.Stop();
            VerifySshConnection();
        }

        internal virtual void ConnectScp(long timeout)
        {
            ConnectScp(timeout, GetPasswordConnectionInfo());
        }

        internal virtual void ConnectScp(long timeout, ConnectionInfo connectionInfo)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (_scpClient == null)
                _scpClient = new ScpClient(connectionInfo)
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(KeepAliveInterval)
                };

            while (!_scpClient.IsConnected && stopwatch.ElapsedMilliseconds < timeout * 1000)
            {
                try
                {
                    if (VerboseLoggingEnabled)
                        Log.Debug($"Connecting SCP to {IpAddress} on port {TcpPort}");

                    _scpClient.Connect();
                }
                catch (SocketException ex)
                {
                    Log.Debug(ex.ToString());
                }

                if (!_scpClient.IsConnected)
                    TapThread.Sleep(5000);
            }

            stopwatch.Stop();
            VerifyScpConnection();
        }

        public virtual void Disconnect()
        {
            DisconnectSsh();
            DisconnectScp();
            IsConnected = false;
        }

        public void UploadFiles(List<(string localFileName, string remoteFileName)> files)
        {
            throw new NotImplementedException();
        }

        public void DownloadFiles(List<(string remoteFileName, string localFileName)> files)
        {
            throw new NotImplementedException();
        }

        protected void DisconnectSsh()
        {
            if (_sshClient == null)
                return;

            if (VerboseLoggingEnabled)
                Log.Debug($"Disconnecting SSH from {IpAddress}");

            _sshClient.Disconnect();
            _sshClient.Dispose();
            _sshClient = null;
        }

        protected void DisconnectScp()
        {
            if (_scpClient == null)
                return;

            if (VerboseLoggingEnabled)
                Log.Debug($"Disconnecting SCP from {IpAddress}");

            _scpClient.Disconnect();
            _scpClient.Dispose();
            _scpClient = null;
        }

        protected void VerifySshConnection()
        {
            if (_sshClient == null || !_sshClient.IsConnected)
                throw new InvalidOperationException($"{Name} (ssh client) is not connected");
        }

        protected void VerifyScpConnection()
        {
            if (_scpClient == null || !_scpClient.IsConnected)
                throw new InvalidOperationException($"{Name} (scp client) is not connected");
        }

        public virtual bool SendSshQuery(string command, int timeout, out string response)

        {
            VerifySshConnection();

            bool success;
            lock (SshLock)
            {
                using (var shell = _sshClient.CreateShellStream("sshCommand", 800, 24, 8000, 600, 1024))
                {
                    success = SendCommand(shell, command, timeout, out response);
                }
            }

            return success;
        }

        private bool SendCommand(ShellStream stream, string command, int timeout, out string response)
        {
            var writer = new StreamWriter(stream) { AutoFlush = true };
            WriteStream(command + "; echo Exit Status for my own command:$?", writer, stream);
            // create reader after writer so write command is not in read stream
            var reader = new StreamReader(stream);

            // return ReadStream(reader, timeout, out response);
            return ReadStreamAbsResponse(reader, timeout, out response);
        }

        protected virtual void WriteStream(string cmd, StreamWriter writer, ShellStream stream)
        {
            Log.Debug($"SSH >> {cmd}");
            writer.WriteLine(cmd);
            while (stream.Length == 0) TapThread.Sleep(500); // ToDo: sleep seems too long. Maybe 100ms
        }

        protected virtual bool ReadStream(StreamReader reader, int timeout, out string response)
        {
            var success = false;
            var readBuffer = new StringBuilder();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (true)
            {
                if (stopwatch.ElapsedMilliseconds > timeout * 1000)
                    throw new TimeoutException(
                        "Timeout occurred while waiting for SSH command to end!");

                string line;
                if ((line = reader.ReadLine()) == null)
                {
                    TapThread.Sleep(100);
                    continue;
                }

                readBuffer.AppendLine(line);

                // if (VerboseLogging)
                if (!string.IsNullOrWhiteSpace(line))
                    Log.Debug($"SSH << {line}");

                // Ignore the sent command line
                if (line.Contains("Exit Status for my own command:$")) continue;

                // If the echoed Exit status is not found then just continue reading lines.
                if (!line.Contains("Exit Status for my own command:")) continue;

                var statusLine = line.Split(':');
                if (statusLine.Length > 1 && statusLine[1] == "0") success = true;

                break;
            }

            stopwatch.Stop();
            response = readBuffer.ToString();
            return success;
        }

        protected virtual bool ReadStreamAbsResponse(StreamReader reader, int timeout, out string response)
        {
            var buildFlag = false;
            var success = false;
            var readBuffer = new StringBuilder();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (true)
            {
                if (stopwatch.ElapsedMilliseconds > timeout * 1000)
                    throw new TimeoutException(
                        "Timeout occurred while waiting for SSH command to end!");

                string line;
                if ((line = reader.ReadLine()) == null)
                {
                    TapThread.Sleep(100);
                    continue;
                }

                // Start building the absolute DUT response to command after this line
                if (line.Contains("Exit Status for my own command:$"))
                {
                    buildFlag = true;
                    continue;
                }

                // Build the response until the very last line of ShellStream is reached
                // Log the lines since the response can have linebreaks but TAP supports only oneliners.
                if (!line.Contains("Exit Status for my own command:"))
                {
                    if (buildFlag)
                    {
                        readBuffer.AppendLine(line);

                        if (!string.IsNullOrWhiteSpace(line))
                            Log.Debug($"SSH << {line}");
                    }

                    continue;
                }

                var statusLine = line.Split(':');
                if (statusLine.Length > 1 && statusLine[1] == "0") success = true;

                break;
            }

            stopwatch.Stop();
            response = readBuffer.ToString().Trim();
            return success;
        }
    }
}