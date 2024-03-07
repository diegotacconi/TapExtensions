using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using OpenTap;
using Renci.SshNet;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Duts.Ssh
{
    [Display("DutSshShell",
        Groups: new[] { "TapExtensions", "Duts", "Ssh" })]
    public class DutSshShell : Dut, ISsh
    {
        #region Settings

        [Display("Host", Order: 1, Group: "SSH Settings", Description: "Host name (or IP address)")]
        public string Host { get; set; }

        [Display("Port", Order: 2, Group: "SSH Settings", Description: "TCP port number (default value is 22)")]
        public int Port { get; set; }

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
        private readonly object _sshLock = new object();

        public DutSshShell()
        {
            // Default values
            Name = nameof(DutSshShell);
            Host = "localhost";
            Port = 22;
            Username = "root";
            Password = "";
            KeepAliveInterval = 30;

            // Validation rules
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
                _sshClient = new SshClient(Host, Port, Username, Password)
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(KeepAliveInterval)
                };

            if (!_sshClient.IsConnected)
            {
                if (VerboseLoggingEnabled)
                    Log.Debug($"Connecting to {Host} on port {Port}");

                _sshClient.Connect();
                IsConnected = true;
            }
        }

        public void Disconnect()
        {
            if (_sshClient == null)
                return;

            if (VerboseLoggingEnabled)
                Log.Debug($"Disconnecting from {Host}");

            _sshClient.Disconnect();
            _sshClient.Dispose();
            _sshClient = null;
            IsConnected = false;
        }

        public string Query(string command, int timeout)
        {
            if (_sshClient == null || !_sshClient.IsConnected)
                throw new InvalidOperationException($"{Name} is not connected");

            string response;
            lock (_sshLock)
            {
                using (var shell = _sshClient.CreateShellStream("SshShell", 0, 0, 0, 0, 1024))
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    // Write command
                    WriteStream(command, shell, stopwatch, timeout);

                    // Read response
                    response = ReadStream(shell, stopwatch, timeout);

                    stopwatch.Stop();
                }
            }

            return response;
        }

        private void WriteStream(string command, Stream shell, Stopwatch stopwatch, int timeout)
        {
            var writer = new StreamWriter(shell) { AutoFlush = true };
            var cmd = command + "; echo Exit Status for my own command:$?";
            Log.Debug($"SSH >> {cmd}");
            writer.WriteLine(cmd);
            while (shell.Length == 0)
            {
                TapThread.Sleep(20);
                if (stopwatch.Elapsed > TimeSpan.FromSeconds(timeout))
                    throw new InvalidOperationException(
                        $"Timeout occurred while sending ssh command of '{command}'");
            }
        }

        private string ReadStream(Stream shell, Stopwatch stopwatch, int timeout)
        {
            var buildFlag = false;
            var success = false;
            var response = new StringBuilder();
            response.Clear();

            var reader = new StreamReader(shell);
            while (true)
            {
                if (timeout > 0 && stopwatch.Elapsed > TimeSpan.FromSeconds(timeout))
                    throw new InvalidOperationException(
                        "Timeout occurred while waiting for ssh response to end");

                string line;
                if ((line = reader.ReadLine()) == null)
                {
                    TapThread.Sleep(10);
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
                        response.AppendLine(line);

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

            if (!success)
                throw new InvalidOperationException(
                    "Error occurred in executing ssh command");

            return response.ToString();
        }

        public void UploadFiles(List<(string localFilePath, string remoteFilePath)> files)
        {
            throw new NotImplementedException();
        }

        public void DownloadFiles(List<(string remoteFilePath, string localFilePath)> files)
        {
            throw new NotImplementedException();
        }
    }
}