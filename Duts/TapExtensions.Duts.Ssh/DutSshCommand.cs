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
    [Display("DutSshCommand",
        Groups: new[] { "TapExtensions", "Duts", "Ssh" })]
    public class DutSshCommand : Dut, ISsh
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

        public DutSshCommand()
        {
            // Default values
            Name = nameof(DutSshCommand);
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

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // https://stackoverflow.com/questions/47386713/execute-long-time-command-in-ssh-net-and-display-the-results-continuously-in-tex

            var cmd = _sshClient.CreateCommand(command);
            cmd.CommandTimeout = TimeSpan.FromSeconds(timeout);

            Log.Debug($"SSH >> {cmd.CommandText}");
            var async = cmd.BeginExecute(ar => stopwatch.Stop());

            // var stdoutStreamReader = new StreamReader(cmd.OutputStream);
            // var stderrStreamReader = new StreamReader(cmd.ExtendedOutputStream);

            var readBuffer = new StringBuilder();
            // var lineBuffer = new StringBuilder();
            using (var reader = new StreamReader(cmd.OutputStream, Encoding.UTF8, true, 1024, true))
            {
                while (!async.IsCompleted || !reader.EndOfStream)
                {
                    if (timeout > 0 && stopwatch.Elapsed > TimeSpan.FromSeconds(timeout))
                        throw new InvalidOperationException(
                            "Timeout occurred while waiting for ssh response to end");

                    // var line = reader.ReadLine();
                    var line = reader.ReadToEnd();
                    if (!string.IsNullOrEmpty(line))
                    {
                        readBuffer.Append(line);

                        // ToDo: call LogLineByLine()

                        if (!string.IsNullOrWhiteSpace(line))
                            Log.Debug($"SSH << {line}");
                    }
                }

                // LogBytes(Encoding.UTF8.GetBytes(readBuffer.ToString()));
            }

            cmd.EndExecute(async);

            if (cmd.ExitStatus != 0)
                throw new InvalidOperationException(cmd.Error);

            return readBuffer.ToString();
        }

        /*
        private void LogBytes(byte[] bytes)
        {
            if (VerboseLoggingEnabled)
            {
                var sb1 = new StringBuilder();
                var sb2 = new StringBuilder();
                foreach (var c in bytes)
                {
                    sb1.Append(c.ToString("X2") + " ");

                    var j = c;
                    if (j >= 0x20 && j <= 0x7E)
                        sb2.Append((char) j);
                    else
                        sb2.Append('.');

                    sb2.Append("  ");
                }

                Log.Debug($"Debug - Hex:   {sb1}");
                Log.Debug($"Debug - Ascii: {sb2}");
            }
        }
        */

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