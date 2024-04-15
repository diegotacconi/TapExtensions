using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using OpenTap;
using Renci.SshNet;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Duts.Ssh
{
    [Display("SshCommandDut",
        Groups: new[] { "TapExtensions", "Duts", "Ssh" })]
    public class SshCommandDut : Dut, ISecureShell
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

        public SshCommandDut()
        {
            // Default values
            Name = nameof(SshCommandDut);
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

        public void Connect()
        {
            if (_sshClient == null)
                _sshClient = new SshClient(IpAddress, TcpPort, Username, Password)
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(KeepAliveInterval)
                };

            if (!_sshClient.IsConnected)
            {
                if (VerboseLoggingEnabled)
                    Log.Debug($"Connecting to {IpAddress} on port {TcpPort}");

                _sshClient.Connect();
                IsConnected = true;
            }
        }

        public void Disconnect()
        {
            if (_sshClient == null)
                return;

            if (VerboseLoggingEnabled)
                Log.Debug($"Disconnecting from {IpAddress}");

            _sshClient.Disconnect();
            _sshClient.Dispose();
            _sshClient = null;
            IsConnected = false;
        }

        private void ScpConnect()
        {
            if (_scpClient == null)
                _scpClient = new ScpClient(IpAddress, TcpPort, Username, Password)
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(KeepAliveInterval)
                };

            if (!_scpClient.IsConnected)
            {
                if (VerboseLoggingEnabled)
                    Log.Debug($"SCP: Connecting to {IpAddress} on port {TcpPort}");

                _scpClient.Connect();
            }
        }

        private void ScpDisconnect()
        {
            if (_scpClient == null)
                return;

            if (VerboseLoggingEnabled)
                Log.Debug($"SCP: Disconnecting from {IpAddress}");

            _scpClient.Disconnect();
            _scpClient.Dispose();
            _scpClient = null;
        }

        public void UploadFiles(List<(string localFile, string remoteFile)> files)
        {
            if (files.Count == 0)
                throw new ArgumentException(@"List of files cannot be empty", nameof(files));

            // Connect
            ScpConnect();

            // Transfer files
            foreach (var (localFile, remoteFile) in files)
            {
                if (string.IsNullOrWhiteSpace(localFile))
                    throw new InvalidOperationException("Local filename cannot be empty");
                if (string.IsNullOrWhiteSpace(remoteFile))
                    throw new InvalidOperationException("Remote filename cannot be empty");
                if (!File.Exists(localFile))
                    throw new FileNotFoundException($"The file {localFile} could not be found");

                if (VerboseLoggingEnabled)
                    Log.Debug($"SCP: Transferring file from {localFile} to {remoteFile}");

                _scpClient.Upload(new FileInfo(localFile), remoteFile);
            }

            // Disconnect
            ScpDisconnect();
        }

        public void DownloadFiles(List<(string remoteFile, string localFile)> files)
        {
            if (files.Count == 0)
                throw new ArgumentException(@"List of files cannot be empty", nameof(files));

            // Connect
            ScpConnect();

            // Transfer files
            foreach (var (remoteFile, localFile) in files)
            {
                if (string.IsNullOrWhiteSpace(localFile))
                    throw new InvalidOperationException("Local filename cannot be empty");
                if (string.IsNullOrWhiteSpace(remoteFile))
                    throw new InvalidOperationException("Remote filename cannot be empty");

                if (VerboseLoggingEnabled)
                    Log.Debug($"SCP: Transferring file from {remoteFile} to {localFile}");

                _scpClient.Download(remoteFile, new FileInfo(localFile));
            }

            // Disconnect
            ScpDisconnect();
        }

        public bool SendSshQuery(string command, int timeout, out string response)
        {
            if (_sshClient == null || !_sshClient.IsConnected)
                throw new InvalidOperationException($"{Name} is not connected");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var cmd = _sshClient.CreateCommand(command);
            cmd.CommandTimeout = TimeSpan.FromSeconds(timeout);

            if (VerboseLoggingEnabled)
                Log.Debug($"SSH >> {cmd.CommandText}");

            var async = cmd.BeginExecute(ar => stopwatch.Stop());

            // var stderrStreamReader = new StreamReader(cmd.ExtendedOutputStream);

            var readBuffer = new StringBuilder();
            using (var reader = new StreamReader(cmd.OutputStream, Encoding.UTF8, true, 1024, true))
            {
                while (!async.IsCompleted || !reader.EndOfStream)
                {
                    if (timeout > 0 && stopwatch.Elapsed > TimeSpan.FromSeconds(timeout))
                        throw new InvalidOperationException(
                            "Timeout occurred while waiting for ssh response to end");

                    var readPart = reader.ReadToEnd();

                    if (string.IsNullOrEmpty(readPart))
                    {
                        TapThread.Sleep(1);
                        continue; // Go to the next while iteration
                    }

                    readBuffer.Append(readPart);

                    if (!VerboseLoggingEnabled)
                        continue; // Go to the next while iteration

                    // Split into lines
                    var lines = readPart.Split(new[] { "\r\n", "\n\r", "\r", "\n" },
                        StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue; // Go to the next foreach line

                        // Remove ANSI escape codes from log message
                        var lineWithoutAnsiEscapeCodes =
                            Regex.Replace(line, @"\x1B\[[^@-~]*[@-~]", "", RegexOptions.Compiled);

                        var msg = $"SSH << {lineWithoutAnsiEscapeCodes}";

                        // Truncate long message to a maximum sting length
                        const int maxLength = 500;
                        if (msg.Length > maxLength)
                            msg = msg.Substring(0, maxLength) + "***";

                        Log.Debug(msg);
                    }
                }
            }

            cmd.EndExecute(async);

            // if (cmd.ExitStatus != 0)
            //     throw new InvalidOperationException(cmd.Error);

            response = readBuffer.ToString();
            return cmd.ExitStatus == 0;
        }
    }
}