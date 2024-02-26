using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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

        [Display("DUT IP", Order: 1, Group: "SSH Settings")]
        public string DutIp { get; set; } = "";

        [Display("DUT Frmon port", Order: 2, Group: "SSH Settings")]
        public uint Port { get; set; } = 22;

        [Display("DUT Frmon timeout", Order: 3, Group: "SSH Settings")]
        [Unit("ms")]
        public uint Timeout { get; set; } = 5000;

        [Display("DUT Ssh port", Order: 4, Group: "SSH Settings")]
        public uint SshPort { get; set; } = 22;

        [Display("DUT Ssh user name", Order: 5, Group: "SSH Settings")]
        public string DutUserName { get; set; } = "";

        [Display("DUT Ssh user password", Order: 6, Group: "SSH Settings")]
        public string DutPassword { get; set; }

        [Display("DUT Ssh keep alive time", Order: 7, Group: "SSH Settings")]
        [Unit("s")]
        public int SshKeepAliveSeconds { get; set; } = 5;

        [Display("Verbose Logging", Order: 99, Group: "Debug", Collapsed: true,
            Description: "Enables verbose logging of SSH communication.")]
        public bool VerboseLogging { get; set; } = true;

        #endregion

        protected readonly object SshLock = new object();
        protected const string SshInPrefix = "SSH << ";
        internal HashSet<string> InitializedFrmonHashSet;
        protected TimeSpan SshKeepAlive;
        private SshClient _sshClient;
        private ScpClient _scpClient;

        public DutControlSsh()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            InitializedFrmonHashSet = new HashSet<string>();
            SshKeepAlive = new TimeSpan(0, 0, SshKeepAliveSeconds);
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
            const int timeoutMs = 5000;
            ConnectSsh(timeoutMs);
            ConnectScp(timeoutMs);

            IsConnected = true;
        }

        private ConnectionInfo GetPasswordConnectionInfo()
        {
            var authenticationMethods = new List<AuthenticationMethod>
            {
                new PasswordAuthenticationMethod(DutUserName, DutPassword)
            };

            var passwordConnectionInfo = new ConnectionInfo(DutIp, DutUserName, authenticationMethods.ToArray());
            return passwordConnectionInfo;
        }

        internal virtual void ConnectSsh(long timeoutMs)
        {
            ConnectSsh(timeoutMs, GetPasswordConnectionInfo());
        }

        internal virtual void ConnectSsh(long timeoutMs, ConnectionInfo connectionInfo)
        {
            var connectTimeoutSw = new Stopwatch();
            connectTimeoutSw.Start();

            if (_sshClient == null)
                _sshClient = new SshClient(connectionInfo)
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(SshKeepAliveSeconds)
                };

            while (!_sshClient.IsConnected && connectTimeoutSw.ElapsedMilliseconds < timeoutMs)
            {
                try
                {
                    if (VerboseLogging)
                        Log.Debug($"Connecting SSH to {DutIp} on port {Port}");

                    _sshClient.Connect();
                }
                catch (SocketException ex)
                {
                    Log.Debug(ex.ToString());
                }

                if (!_sshClient.IsConnected)
                    TapThread.Sleep(5000);
            }

            VerifySshConnection();
        }

        internal virtual void ConnectScp(long timeoutMs)
        {
            ConnectScp(timeoutMs, GetPasswordConnectionInfo());
        }

        internal virtual void ConnectScp(long timeoutMs, ConnectionInfo connectionInfo)
        {
            var connectTimeoutSw = new Stopwatch();
            connectTimeoutSw.Start();

            if (_scpClient == null)
                _scpClient = new ScpClient(connectionInfo)
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(SshKeepAliveSeconds)
                };

            while (!_scpClient.IsConnected && connectTimeoutSw.ElapsedMilliseconds < timeoutMs)
            {
                try
                {
                    if (VerboseLogging)
                        Log.Debug($"Connecting SCP to {DutIp} on port {Port}");

                    _scpClient.Connect();
                }
                catch (SocketException ex)
                {
                    Log.Debug(ex.ToString());
                }

                if (!_scpClient.IsConnected)
                    TapThread.Sleep(5000);
            }

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

            if (VerboseLogging)
                Log.Debug($"Disconnecting SSH from {DutIp}");

            _sshClient.Disconnect();
            _sshClient.Dispose();
            _sshClient = null;
        }

        protected void DisconnectScp()
        {
            if (_scpClient == null)
                return;

            if (VerboseLogging)
                Log.Debug($"Disconnecting SCP from {DutIp}");

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

        public void SendSshCommands(string[] commands, int timeoutMs)
        {
            SendSshCommands(commands, timeoutMs, false);
        }

        public void SendSshCommands(string[] commands, int timeoutMs, bool runAsRoot)
        {
            VerifySshConnection();
            lock (SshLock)
            {
                using (var stream = _sshClient.CreateShellStream("sshCommand", 800, 24, 8000, 600, 1024))
                {
                    if (runAsRoot) SwitchToRoot(stream);

                    foreach (var command in commands)
                    {
                        if (string.IsNullOrEmpty(command))
                        {
                            Log.Warning("Null or Empty command found in array, ignore and continue");
                            continue;
                        }

                        if (SendCommand(stream, command, timeoutMs, out _)) continue;
                        var error = $"Error occurred in executing command: {command}.";
                        throw new InvalidOperationException(error);
                    }
                }
            }
        }

        private void SwitchToRoot(ShellStream stream)
        {
            // Send command and expect password or user prompt
            stream.WriteLine("sudo -i");
            var prompt = stream.Expect(new Regex(@"([$#>:])"));
            Log.Debug(prompt);
        }

        public string SendSshQuery(string command, int timeoutMs)
        {
            return SendSshQuery(command, timeoutMs, false);
        }

        /// <summary>
        ///     Return the absolute response that is directly resulted from the SSH command.
        ///     Additional white-space characters are trimmed from both ends, but the response can contain line breaks.
        /// </summary>
        private string SendSshQuery(string command, int timeoutMs, bool absoluteResponse)
        {
            VerifySshConnection();
            string response;

            lock (SshLock)
            {
                using (var stream = _sshClient.CreateShellStream("sshCommand", 800, 24, 8000, 600, 1024))
                {
                    if (!SendCommand(stream, command, timeoutMs, absoluteResponse, out response))
                    {
                        var error = $"Error occurred in executing command: {command}";
                        throw new InvalidOperationException(error);
                    }
                }
            }

            return response;
        }

        private bool SendCommand(ShellStream stream, string command, int timeoutMs, bool absoluteResponse,
            out string response)
        {
            var writer = new StreamWriter(stream) { AutoFlush = true };
            WriteStream(command + "; echo Exit Status for my own command:$?", writer, stream);
            // create reader after writer so write command is not in read stream
            var reader = new StreamReader(stream);

            return absoluteResponse
                ? ReadStreamAbsResponse(reader, timeoutMs, out response)
                : ReadStream(reader, timeoutMs, out response);
        }

        private bool SendCommand(ShellStream stream, string command, int timeoutMs, out string response)
        {
            return SendCommand(stream, command, timeoutMs, false, out response);
        }

        protected virtual void WriteStream(string cmd, StreamWriter writer, ShellStream stream)
        {
            Log.Info("SSH Write: " + cmd);
            writer.WriteLine(cmd);
            while (stream.Length == 0) TapThread.Sleep(500);
        }

        protected virtual bool ReadStream(StreamReader reader, int timeoutMs, out string response)
        {
            var success = false;
            var result = new StringBuilder();
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            while (true)
            {
                if (timeoutMs < stopWatch.ElapsedMilliseconds)
                    throw new TimeoutException("Timeout occurred when waiting for SSH command to end!");

                string line;
                if ((line = reader.ReadLine()) == null)
                {
                    TapThread.Sleep(100);
                    continue;
                }

                // if (VerboseLogging) Log.Info(SshInPrefix + line);
                Log.Info("SSH read: " + line);

                result.AppendLine(line);

                // Ignore the sent command line
                if (line.Contains("Exit Status for my own command:$")) continue;

                // If the echoed Exit status is not found then just continue reading lines.
                if (!line.Contains("Exit Status for my own command:")) continue;

                var statusLine = line.Split(':');
                if (statusLine.Length > 1 && statusLine[1] == "0") success = true;

                break;
            }

            response = result.ToString();
            return success;
        }

        /// <summary>
        ///     Read and log the response of DUT to a command, containing only lines that are directly
        ///     resulted from the sent command.
        /// </summary>
        private bool ReadStreamAbsResponse(StreamReader reader, int timeoutMs, out string response)
        {
            var buildFlag = false;
            var success = false;
            var result = new StringBuilder();
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            while (true)
            {
                if (timeoutMs < stopWatch.ElapsedMilliseconds)
                    throw new TimeoutException("Timeout occurred when waiting for SSH command to end!");

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
                        result.AppendLine(line);
                        Log.Info($"DUT response: {line}");
                    }

                    continue;
                }

                var statusLine = line.Split(':');
                if (statusLine.Length > 1 && statusLine[1] == "0") success = true;

                break;
            }

            response = result.ToString().Trim();
            return success;
        }

        protected virtual bool ReadStreamWaitAnswer(StreamReader reader, int timeoutMs, out string response,
            string expectedAnswer, string searchSeparator)
        {
            var result = new StringBuilder();
            var stopWatch = new Stopwatch();

            var listOfString = expectedAnswer.Split(searchSeparator.ToCharArray());

            stopWatch.Start();

            while (true)
            {
                if (timeoutMs < stopWatch.ElapsedMilliseconds)
                    throw new TimeoutException("Timeout occurred when waiting for SSH command to end!");

                string line;
                if ((line = reader.ReadLine()) == null)
                {
                    TapThread.Sleep(100);
                    continue;
                }

                if (VerboseLogging) Log.Info(SshInPrefix + line);

                result.AppendLine(line);

                var ret = false;
                var resultsString = result.ToString();
                foreach (var s in listOfString)
                {
                    ret = resultsString.Contains(s);
                    if (ret) continue;
                    Log.Info($"Cannot find \"{s}\" from DUT response.");
                    break;
                }

                if (ret) break;
            }

            response = result.ToString();
            return true;
        }
    }
}