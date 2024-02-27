using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using OpenTap;
using Renci.SshNet;
using TapExtensions.Interfaces.Duts;

namespace TapExtensions.Duts.DutControl
{
    [Display("DutControlSsh",
        Groups: new[] { "TapExtensions", "Duts", "DutControl" })]
    public class DutControlSsh : Dut, IDutControlSsh
    {
        #region Settings

        [Display("Host", Order: 1, Group: "SSH Settings", Description: "Host name (or IP address)")]
        public string Host { get; set; } = "localhost";

        [Display("Port", Order: 2, Group: "SSH Settings", Description: "TCP port number (default value is 22)")]
        public int Port { get; set; } = 22;

        [Display("Username", Order: 3, Group: "SSH Settings")]
        public string Username { get; set; } = "root";

        [Display("Password", Order: 4, Group: "SSH Settings")]
        public string Password { get; set; } = "";

        [Display("Keep Alive Interval", Order: 7, Group: "SSH Settings")]
        [Unit("s")]
        public int KeepAliveInterval { get; set; } = 5;

        [Display("Verbose SSH Logging", Order: 6, Group: "Debug", Collapsed: true,
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
            DisconnectDut();
            base.Close();
            IsConnected = false;
        }

        public virtual void ConnectDut()
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

            var passwordConnectionInfo = new ConnectionInfo(Host, Username, authenticationMethods.ToArray());
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
                        Log.Debug($"Connecting SSH to {Host} on port {Port}");

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
                        Log.Debug($"Connecting SCP to {Host} on port {Port}");

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

        public virtual void DisconnectDut()
        {
            DisconnectSsh();
            DisconnectScp();
            IsConnected = false;
        }

        protected void DisconnectSsh()
        {
            if (_sshClient == null)
                return;

            if (VerboseLoggingEnabled)
                Log.Debug($"Disconnecting SSH from {Host}");

            _sshClient.Disconnect();
            _sshClient.Dispose();
            _sshClient = null;
        }

        protected void DisconnectScp()
        {
            if (_scpClient == null)
                return;

            if (VerboseLoggingEnabled)
                Log.Debug($"Disconnecting SCP from {Host}");

            _scpClient.Disconnect();
            _scpClient.Dispose();
            _scpClient = null;
        }

        protected void VerifySshConnection()
        {
            if (_sshClient == null) throw new InvalidOperationException(nameof(_sshClient) + " is null!");
            if (!_sshClient.IsConnected) throw new InvalidOperationException("Ssh client not connected!");
        }

        protected void VerifyScpConnection()
        {
            if (_scpClient == null) throw new InvalidOperationException(nameof(_scpClient) + " is null!");
            if (!_scpClient.IsConnected) throw new InvalidOperationException("Scp client not connected!");
        }

        public string SendSshQuery(string command, int timeout)
        {
            VerifySshConnection();
            string response;

            lock (SshLock)
            {
                using (var stream = _sshClient.CreateShellStream("sshCommand", 800, 24, 8000, 600, 1024))
                {
                    if (!SendCommand(stream, command, timeout, out response))
                        throw new InvalidOperationException(
                            $"Error occurred in executing command: {command}");
                }
            }

            return response;
        }

        private bool SendCommand(ShellStream stream, string command, int timeout, out string response)
        {
            var writer = new StreamWriter(stream) { AutoFlush = true };
            WriteStream(command + "; echo Exit Status for my own command:$?", writer, stream);
            // create reader after writer so write command is not in read stream
            var reader = new StreamReader(stream);

            return ReadStream(reader, timeout, out response);
        }

        protected virtual void WriteStream(string cmd, StreamWriter writer, ShellStream stream)
        {
            Log.Debug($"SSH >> {cmd}");
            writer.WriteLine(cmd);
            while (stream.Length == 0) TapThread.Sleep(500);
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
                        "Timeout occurred when waiting for SSH command to end!");

                string line;
                if ((line = reader.ReadLine()) == null)
                {
                    TapThread.Sleep(100);
                    continue;
                }

                // if (VerboseLogging)
                if (!string.IsNullOrWhiteSpace(line))
                    Log.Debug($"SSH << {line}");

                readBuffer.AppendLine(line);

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
    }
}