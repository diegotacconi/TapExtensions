using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using OpenTap;
using Renci.SshNet;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Duts.Ssh
{
    [Display("SshDut",
        Groups: new[] {"TapExtensions", "Duts", "Ssh"})]
    public class SshDut : Dut, ISsh
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

        public SshDut()
        {
            // Default values
            Name = nameof(SshDut);
            Host = "localhost";
            Port = 22;
            Username = "root";
            Password = "";
            KeepAliveInterval = 30;

            // Validation rules
            Rules.Add(() => KeepAliveInterval >= 0,
                "Time interval must be greater than or equal to zero", nameof(KeepAliveInterval));
        }

        public override void Close()
        {
            Disconnect();
            base.Close();
        }

        public void Connect()
        {
            if (_sshClient == null)
            {
                _sshClient = new SshClient(Host, Port, Username, Password)
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(KeepAliveInterval)
                };
            }

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
            if (_sshClient == null)
                throw new InvalidOperationException($"{nameof(_sshClient)} is null");

            if (!_sshClient.IsConnected)
                throw new InvalidOperationException($"{nameof(_sshClient)} is not connected");

            var timer = new Stopwatch();
            timer.Start();

            // https://stackoverflow.com/questions/47386713/execute-long-time-command-in-ssh-net-and-display-the-results-continuously-in-tex

            var cmd = _sshClient.CreateCommand(command);

            Log.Debug($"SSH >> {cmd.CommandText}");
            var async = cmd.BeginExecute(ar => timer.Stop());

            // var stdoutStreamReader = new StreamReader(cmd.OutputStream);
            // var stderrStreamReader = new StreamReader(cmd.ExtendedOutputStream);

            var readBuffer = new StringBuilder();
            // var lineBuffer = new StringBuilder();
            using (var reader = new StreamReader(cmd.OutputStream, Encoding.UTF8, true, 1024, true))
            {
                while (!async.IsCompleted || !reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line != null)
                    {
                        readBuffer.Append(line + "\n");

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
        public List<string> QueryToList(string command, int timeout)
        {
            var lines = new List<string>();

            if (_sshClient == null)
                throw new InvalidOperationException($"{nameof(_sshClient)} is null");

            if (!_sshClient.IsConnected)
                throw new InvalidOperationException($"{nameof(_sshClient)} is not connected");

            var cmd = _sshClient.CreateCommand(command);
            Log.Debug($"SSH >> {cmd.CommandText}");
            var async = cmd.BeginExecute();

            using (var reader = new StreamReader(cmd.OutputStream, Encoding.UTF8, true, 1024, true))
            {
                while (!async.IsCompleted || !reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        lines.Add(line);
                        Log.Debug($"SSH << {line}");
                        // LogBytes(Encoding.UTF8.GetBytes(line));
                    }
                }
            }

            cmd.EndExecute(async);

            if (cmd.ExitStatus != 0)
                throw new InvalidOperationException(cmd.Error);

            // Check if the list of response lines from the ssh command is empty
            var isEmpty = !lines.Any();
            if (isEmpty)
                lines = new List<string> {string.Empty};

            return lines;
        }
        */

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
    }
}