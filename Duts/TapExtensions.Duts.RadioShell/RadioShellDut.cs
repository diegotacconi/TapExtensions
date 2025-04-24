using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;
using OpenTap;
using Renci.SshNet;
using TapExtensions.Interfaces.Ssh;
using TraceSource = OpenTap.TraceSource;

namespace TapExtensions.Duts.RadioShell
{
    [Display("RadioShellDut",
        Groups: new[] { "TapExtensions", "Duts", "RadioShell" })]
    public class RadioShellDut : Dut, IRadioShell
    {
        #region IRadioShell

        private const string RadioConnectionSettings = "Radio Connection Settings";

        internal HashSet<string> InitializedRadioHashSet;

        [XmlIgnore] public virtual IRadioAccess DutRadioAccess { get; set; }

        [Display("DUT Radio port", Group: RadioConnectionSettings, Order: 2)]
        public virtual uint Port { get; set; }

        [Display("DUT Radio command timeout", Group: RadioConnectionSettings, Order: 2.1,
            Description: "Timeout for each Radio command.")]
        [Unit("ms")]
        public virtual uint TimeoutMs { get; set; } = 40000;

        [Display("Radio connection open timeout", Group: RadioConnectionSettings, Order: 2.4,
            Description: "Timeout how long Radio interface availability is polled.")]
        [Unit("ms")]
        public virtual uint RadioOpenTimeoutMs { get; set; } = 20000;

        [Display("Radio Open Retry Delay ", Group: RadioConnectionSettings, Order: 2.7, Description:
            "Timeout how long is waited before connection is tried to open again in case of failed to open the connection.")]
        [Unit("ms")]
        public virtual uint RadioOpenRetryDelay { get; set; } = 3000;

        [Display("Command Prompt Ready Regex", Group: RadioConnectionSettings, Order: 2.7, Description:
            "Define a string to determine when command prompt is ready while Radio shell stream is opened.\r" +
            "Example: rfsw@simics-mars|root@simics-mars")]
        public virtual string CommandPromptRegex { get; set; } = @"#";

        public RadioShellDut()
        {
            Name = nameof(RadioShellDut);
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            InitializedRadioHashSet = new HashSet<string>();
        }

        public virtual void ConnectDutRadio(uint timeOutInMs, uint successfulReplies)
        {
            const uint pingIntervalMs = 1000;
            Log.Debug("--- ConnectDut ---");
            DutRadioAccess?.Close();
            IsConnected = false;
            if (successfulReplies > 0)
            {
                var pingOk = Ping(timeOutInMs, pingIntervalMs, successfulReplies);
                if (!pingOk)
                    throw new TimeoutException("Ping timed out!");
            }

            CreateRadioAccess(Username, Password, Port, TimeoutMs, VerboseLoggingEnabled);
            if (DutRadioAccess == null)
                throw new InvalidOperationException($"{nameof(DutRadioAccess)} is null!");
            DutRadioAccess.Connect(DutIp, Port);
            IsConnected = true;
        }

        public virtual IRadioAccess CreateRadioAccess(string userName, string password, uint port, uint timeOut,
            bool verboseLogging)
        {
            return DutRadioAccess = new RadioAccess
            {
                DutUserName = userName,
                DutPassword = password,
                VerboseLogging = VerboseLoggingEnabled,
                CommandSendTimeoutMs = timeOut,
                OpenTimeoutMs = RadioOpenTimeoutMs,
                SshKeepAlive = new TimeSpan(0, 0, 0, SshKeepAliveInterval, 0),
                ConnectionRetryDelay = RadioOpenRetryDelay,
                DutCommandPromptRegex = CommandPromptRegex
            };
        }

        public virtual string CreateAgentPath(string agentName, string devicePath = "")
        {
            VerifySocketConnection();

            var agentPath = $"/{agentName}{devicePath}";
            if (InitializedRadioHashSet.Contains(agentPath)) return agentPath;
            var command = string.IsNullOrEmpty(devicePath)
                ? string.Join(" ", "/", "action", "create", agentName,
                    agentPath)
                : string.Join(" ", "/", "action", "create", agentName,
                    agentPath, devicePath);

            if (ERadioSuccess.Ok == DutRadioAccess.SendData(command, out var response))
                InitializedRadioHashSet.Add(agentPath);
            else throw new InvalidOperationException("Failed to create agent: " + agentPath + " response: " + response);

            return agentPath;
        }

        public virtual void TerminateAgent(string path)
        {
            VerifySocketConnection();

            var command = string.Join(" ", path, "action", "terminate");
            if (DutRadioAccess.SendData(command, out _) != ERadioSuccess.Ok)
                throw new InvalidOperationException("Failed to terminate agent '" + path + " '!");
            if (InitializedRadioHashSet.Contains(path))
                InitializedRadioHashSet.Remove(path);
        }

        public virtual ERadioSuccess SendRadioCommand(string command, out string response,
            bool returnErrorCode = false)
        {
            var responseCode = DutRadioAccess.SendData(command, out response);
            if (returnErrorCode)
                return responseCode;
            if (responseCode != ERadioSuccess.Ok)
                throw new InvalidOperationException(
                    $"failed for {command}, Radio response: {response}");
            return responseCode;
        }

        protected void VerifySocketConnection()
        {
            if (DutRadioAccess == null) throw new InvalidOperationException(nameof(DutRadioAccess) + " is null!");
            if (!DutRadioAccess.Connected) throw new InvalidOperationException("Radio socket not connected!");
        }

        #endregion

        #region ISecureShell

        public override void Open()
        {
            IsConnected = false;
        }

        public override void Close()
        {
            Disconnect();
            base.Close();
        }

        public virtual bool Ping(uint timeOutMs, uint pingRetryIntervalMs, uint requiredConsecutiveReplies)
        {
            if (requiredConsecutiveReplies <= 0)
                throw new ArgumentOutOfRangeException(nameof(requiredConsecutiveReplies));
            var address = IPAddress.Parse(DutIp);
            var keepOnPinging = true;
            var pingOkReplies = 0;
            var timer = new Stopwatch();

            using (var pingSender = new Ping())
            {
                Log.Debug("Ping for target DUT " + DutIp);
                timer.Start();
                do
                {
                    var pingReply = pingSender.Send(address, 4000);
                    if (pingReply?.Status == IPStatus.Success)
                    {
                        Log.Debug("Ping status: OK");
                        pingOkReplies++;
                        if (pingOkReplies >= requiredConsecutiveReplies) return true;
                    }
                    else
                    {
                        if (pingReply?.Status == IPStatus.TimedOut)
                            Log.Debug("Ping status: No success");

                        pingOkReplies = 0;
                    }

                    TapThread.Sleep((int)pingRetryIntervalMs);

                    if (timer.ElapsedMilliseconds > timeOutMs)
                        keepOnPinging = false;
                } while (keepOnPinging);

                return false;
            }
        }

        public virtual void Connect()
        {
            Disconnect();

            ConnectSsh();
            ConnectScp();

            IsConnected = true;
        }

        public virtual void Disconnect()
        {
            if (VerboseLoggingEnabled)
                Log.Debug($"Disconnecting SSH and SCP from {DutIp}.");
            SshClient?.Disconnect();
            ScpClient?.Disconnect();
            IsConnected = false;
        }

        public virtual void UploadFiles(List<(string localFile, string remoteFile)> files)
        {
            VerifyScpConnection();

            if (files.Count == 0)
                throw new ArgumentException(@"List of files cannot be empty", nameof(files));

            foreach (var (localFile, remoteFile) in files)
            {
                if (string.IsNullOrWhiteSpace(localFile))
                    throw new InvalidOperationException("Local filename cannot be empty");
                if (string.IsNullOrWhiteSpace(remoteFile))
                    throw new InvalidOperationException("Remote filename cannot be empty");
                if (!File.Exists(localFile))
                    throw new FileNotFoundException($"The file {localFile} could not be found");

                Log.Debug($"SCP uploading file from PC {localFile} to DUT {remoteFile}");
                ScpClient.Upload(new FileInfo(localFile), remoteFile);
            }
        }

        public virtual void DownloadFiles(List<(string remoteFile, string localFile)> files)
        {
            VerifyScpConnection();

            if (files.Count == 0)
                throw new ArgumentException(@"List of files cannot be empty", nameof(files));

            foreach (var (remoteFile, localFile) in files)
            {
                if (string.IsNullOrWhiteSpace(localFile))
                    throw new InvalidOperationException("Local filename cannot be empty");
                if (string.IsNullOrWhiteSpace(remoteFile))
                    throw new InvalidOperationException("Remote filename cannot be empty");

                Log.Debug($"SCP downloading file from DUT {remoteFile} to PC {localFile}");
                ScpClient.Download(remoteFile, new FileInfo(localFile));
            }
        }

        public virtual bool SendSshQuery(string command, int timeout, out string response)
        {
            VerifySshConnection();
            var sshCommand = SshClient.CreateCommand(command);
            sshCommand.CommandTimeout = new TimeSpan(0, 0, 0, timeout, 0);
            sshCommand.Execute();
            response = sshCommand.Result;
            return sshCommand.ExitStatus == 0;
        }

        internal virtual void ConnectSsh()
        {
            var connectTimeoutSw = new Stopwatch();
            SshClient = new SshClient(DutIp, Username, Password);
            SshClient.KeepAliveInterval = new TimeSpan(0, 0, 0, SshKeepAliveInterval, 0);
            connectTimeoutSw.Start();
            Log.Debug($"Open SSH connection to {DutIp}");
            do
            {
                try
                {
                    SshClient.Connect();
                    break;
                }
                catch (SocketException ex)
                {
                    Log.Debug(ex.ToString());
                    TapThread.Sleep((int)SshConnectionRetryDelay);
                }
            } while (connectTimeoutSw.ElapsedMilliseconds < SshConnectionTimeout);

            VerifySshConnection();
        }

        internal virtual void ConnectScp()
        {
            var connectTimeoutSw = new Stopwatch();
            ScpClient = new ScpClient(DutIp, Username, Password);
            ScpClient.KeepAliveInterval = new TimeSpan(0, 0, 0, SshKeepAliveInterval, 0);
            ScpClient.OperationTimeout = new TimeSpan(0, 0, 0, 0, ScpOperationTimeout);
            connectTimeoutSw.Start();
            Log.Debug($"Open SCP connection to {DutIp}");
            do
            {
                try
                {
                    ScpClient.Connect();
                    break;
                }
                catch (SocketException ex)
                {
                    Log.Debug(ex.ToString());
                    TapThread.Sleep((int)SshConnectionRetryDelay);
                }
            } while (connectTimeoutSw.ElapsedMilliseconds < SshConnectionTimeout);

            VerifyScpConnection();
        }

        protected void VerifySshConnection()
        {
            if (SshClient == null) throw new InvalidOperationException(nameof(SshClient) + " is null!");
            if (!SshClient.IsConnected) throw new InvalidOperationException("Ssh client not connected!");
        }

        protected void VerifyScpConnection()
        {
            if (ScpClient == null) throw new InvalidOperationException(nameof(ScpClient) + " is null!");
            if (!ScpClient.IsConnected) throw new InvalidOperationException("Scp client not connected!");
        }

        #region Dut Settings Properties

        [XmlIgnore] public SshClient SshClient { get; set; }
        [XmlIgnore] public ScpClient ScpClient { get; set; }

        [Display("IP Address", Group: "SSH Settings", Order: 1)]
        public virtual string DutIp { get; set; }

        [XmlIgnore]
        public virtual string IpAddress
        {
            get => DutIp;
            set => DutIp = value;
        }

        [Display("Port", Group: "SSH Settings", Order: 2)]
        public virtual uint SshPort { get; set; }

        [Display("Username", Group: "SSH Settings", Order: 3)]
        public virtual string Username { get; set; }

        [Display("Password", Group: "SSH Settings", Order: 4)]
        public virtual string Password { get; set; }

        [Display("SSH connection timeout.", Group: "SSH Settings", Order: 4.1)]
        [Unit("msec")]
        public virtual uint SshConnectionTimeout { get; set; } = 60000;

        [Display("Ssh Connection Retry Delay", Group: "SSH Settings",
            Description: "In case of connection establishment failure, set time delay to wait until" +
                         " SSH,SCP or SFTP connection establishment is retried.", Order: 4.14)]
        [Unit("msec")]
        public virtual uint SshConnectionRetryDelay { get; set; } = 5000;

        [Display("Keep Alive Interval", Group: "SSH Settings", Order: 4.4)]
        [Unit("s")]
        public virtual int SshKeepAliveInterval { get; set; } = 5;

        [Display("SCP operation timeout", Group: "SSH Settings", Order: 4.5)]
        [Unit("msec")]
        public virtual int ScpOperationTimeout { get; set; } = 120000;

        [Display("Verbose Logging", Group: "SSH Settings", Order: 99,
            Description: "Enables verbose logging of SSH communication.")]
        public virtual bool VerboseLoggingEnabled { get; set; } = false;

        #endregion

        #endregion
    }

    public class RadioAccess : IRadioAccess
    {
        #region IRadioAccess

        public string DutPassword { get; set; }
        public string DutUserName { get; set; }
        public uint CommandSendTimeoutMs { get; set; } = 40000;
        public uint OpenTimeoutMs { get; set; } = 30000;
        public uint ConnectionRetryDelay { get; set; } = 3000;
        public string DutCommandPromptRegex { get; set; }
        public bool VerboseLogging { get; set; }

        protected readonly object Lock = new object();
        protected TraceSource Logger;
        protected const string InPrefix = "<< ";
        protected const string OutPrefix = ">> ";
        private const string RadioReturnKeyword = "ret ";
        private const string RadioShellClientOpenCmd = "radioShellClient localhost 2000";

        private SshClient ClientSsh { get; set; }
        private ShellStream ShellStream { get; set; }
        private StreamWriter RadioStreamWriter { get; set; }
        private StreamReader RadioStreamReader { get; set; }

        public TimeSpan SshKeepAlive { get; set; } = new TimeSpan(0, 0, 0, 0, 5000);

        private bool IsRadioReady { get; set; }

        public bool Connected
        {
            get
            {
                lock (Lock)
                {
                    return ClientSsh != null && ClientSsh.IsConnected && IsRadioReady;
                }
            }
        }

        public void Connect(string server, uint port)
        {
            lock (Lock)
            {
                if (!IPAddress.TryParse(server, out _))
                    throw new ArgumentException($"Unable to parse {nameof(server)} as an IP address.");
                if (port <= 0)
                    throw new ArgumentException(nameof(port));

                var ip = $"{server.Split('.')[2]}.{server.Split('.')[3]}";
                Logger = Log.CreateSource("Radio_" + ip);

                Close();
                CreateSshClient(server, port);
                if (ClientSsh == null)
                    throw new InvalidOperationException($"Failed to create {nameof(ClientSsh)}");
                ClientSsh.KeepAliveInterval = SshKeepAlive;

                Logger?.Debug("Connecting to Radio via SSH...");
                var connectTimer = new Stopwatch();
                connectTimer.Start();
                do
                {
                    try
                    {
                        ClientSsh.Connect();
                        break;
                    }
                    catch (SocketException ex)
                    {
                        Logger?.Debug(ex.ToString());
                        TapThread.Sleep((int)ConnectionRetryDelay);
                    }
                } while (connectTimer.ElapsedMilliseconds < OpenTimeoutMs);

                if (!ClientSsh.IsConnected)
                    throw new InvalidOperationException("SSH connect timed out to " + server + " port " + port);
                Logger?.Debug($"SSH connected to {server} port {port}");
                CreateShellStream();
                WaitForPrompt();
                OpenRadio();
            }
        }

        internal void CreateShellStream()
        {
            ShellStream = ClientSsh.CreateShellStream("RadioSshCommand", 800, 24, 8000, 600, 1024);
        }

        internal void CreateSshClient(string server, uint port)
        {
            ClientSsh = new SshClient(server, (int)port, DutUserName, DutPassword);
        }

        private void WaitForPrompt()
        {
            var loginText = ShellStream.Expect(new Regex(DutCommandPromptRegex), TimeSpan.FromSeconds(5));
            if (loginText == null)
                throw new InvalidOperationException(
                    $"Shell stream didn't contain valid expression. Check {nameof(DutCommandPromptRegex)}");
        }

        public void Close()
        {
            lock (Lock)
            {
                ClientSsh?.Disconnect();
                ClientSsh?.Dispose();
            }
        }

        public ERadioSuccess SendDataRawResponse(string command, out string response)
        {
            lock (Lock)
            {
                if (string.IsNullOrWhiteSpace(command))
                    throw new ArgumentException(@"Value cannot be null or whitespace.", nameof(command));

                var retTelegram = Send(command, (int)CommandSendTimeoutMs, false);

                response = retTelegram;
                return RadioParser.ParseResponseVerdict(retTelegram, Logger);
            }
        }

        public virtual ERadioSuccess SendData(string command, out string response)
        {
            lock (Lock)
            {
                if (string.IsNullOrWhiteSpace(command))
                    throw new ArgumentException(@"Value cannot be null or whitespace.", nameof(command));

                var retTelegram = Send(command, (int)CommandSendTimeoutMs, false);

                // Cleanup SSH stream response for radio command return telegram
                int retIndex;
                if ((retIndex = retTelegram.IndexOf(RadioReturnKeyword, StringComparison.InvariantCulture)) > -1)
                {
                    retTelegram = retTelegram.Substring(retIndex).TrimEnd();
                    if (retTelegram.EndsWith("$"))
                        retTelegram = retTelegram.Remove(retTelegram.Length - 1, 1);

                    var frmomSuccess = RadioParser.ParseResponse(retTelegram, out response, Logger);
                    if (VerboseLogging && !string.IsNullOrWhiteSpace(response))
                        Logger?.Debug(InPrefix + response);
                    return frmomSuccess;
                }

                response = string.Empty;
                return ERadioSuccess.Error;
            }
        }

        public void SetVerboseLogging(bool verboseLogging)
        {
            VerboseLogging = verboseLogging;
        }

        private string Send(string command, int timeoutMs, bool radioOpen)
        {
            lock (Lock)
            {
                var connectTimer = new Stopwatch();

                if (VerboseLogging)
                {
                    Logger?.Debug(OutPrefix + command);
                    connectTimer.Start();
                }

                RadioStreamReader = new StreamReader(ShellStream);
                RadioStreamReader.ReadToEnd();
                RadioStreamWriter = new StreamWriter(ShellStream);
                RadioStreamWriter.AutoFlush = true;
                WriteStream(command, timeoutMs);

                if (!ReadStream(timeoutMs, out var response, radioOpen))
                    throw new InvalidOperationException($"Error occurred in executing command: {command}");
                if (!VerboseLogging)
                    return response;
                connectTimer.Stop();
                Logger?.Debug(InPrefix + response + $"[{connectTimer.Elapsed.TotalMilliseconds} ms]");
                return response;
            }
        }

        private bool ReadStream(int timeoutMs, out string response, bool radioOpen)
        {
            var success = false;
            var result = new StringBuilder();
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            while (true)
            {
                if (timeoutMs < stopWatch.ElapsedMilliseconds)
                {
                    Logger?.Error("Timeout occurred when waiting for radio command to end!");
                    break;
                }

                var line = RadioStreamReader.ReadToEnd();
                if (string.IsNullOrEmpty(line))
                {
                    TapThread.Sleep(1);
                    continue;
                }

                result.Append(line);
                var radioSignFound = result.ToString().TrimEnd().EndsWith("$");
                if (radioOpen)
                {
                    if (!Regex.IsMatch(result.ToString().TrimEnd(), DutCommandPromptRegex) && !radioSignFound)
                        continue;
                }
                else if (!radioSignFound)
                {
                    continue;
                }

                success = true;
                break;
            }

            response = result.ToString();
            return success;
        }

        private void WriteStream(string cmd, int timeoutMs)
        {
            RadioStreamWriter.WriteLine(cmd);
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (ShellStream.Length == 0)
            {
                if (stopWatch.ElapsedMilliseconds > timeoutMs)
                    throw new TimeoutException(
                        $"Timeout [{timeoutMs}]ms occured while writing cmd: {cmd}. Stream length: {ShellStream.Length}");
                TapThread.Sleep(1);
            }
        }

        private void OpenRadio()
        {
            Logger?.Debug("Starting radio client...");
            IsRadioReady = false;
            var readyTimer = new Stopwatch();
            string response;
            readyTimer.Start();

            do
            {
                response = Send(RadioShellClientOpenCmd, (int)CommandSendTimeoutMs, true).TrimEnd();
                if (response.EndsWith("$") && !response.Contains("connect error"))
                {
                    IsRadioReady = true;
                    break;
                }

                Logger?.Debug("RadioShellClient not ready... trying to connect again!");
                TapThread.Sleep((int)ConnectionRetryDelay);
            } while (readyTimer.ElapsedMilliseconds < OpenTimeoutMs);

            if (!IsRadioReady)
                throw new InvalidOperationException("Failed to start Radio client: " + response);
            Logger?.Debug("Radio client is ready!");
        }

        #endregion
    }
}