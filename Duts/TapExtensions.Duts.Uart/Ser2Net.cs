// https://github.com/cminyard/ser2net

// sudo apt install ser2net
// cat /etc/ser2net.yaml

using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using OpenTap;
using TapExtensions.Interfaces.Uart;
using TapExtensions.Shared.Telnet;

namespace TapExtensions.Duts.Uart
{
    [Display("Ser2Net",
        Groups: new[] { "TapExtensions", "Duts", "Uart" })]
    public class Ser2Net : Dut, IUartDut
    {
        #region Settings

        [Display("IP Address", Order: 1)] public string IpAddress { get; set; }

        [Display("Tcp Port", Order: 2)] public int TcpPort { get; set; }

        #endregion

        private delegate void UartEvent(string readBuffer);

        private event UartEvent ReadEvent;
        private TelnetClient _client;
        private readonly StringBuilder _logBuffer = new StringBuilder();
        private readonly StringBuilder _readBuffer = new StringBuilder();
        private static bool _responseReceived;
        private static string _expectedResponse;
        private readonly ManualResetEvent _waitForEvent = new ManualResetEvent(false);
        private string _response;

        public Ser2Net()
        {
            // Default values
            Name = nameof(Ser2Net);
            IpAddress = "192.168.4.100";
            TcpPort = 3000;

            // Validation rules
            Rules.Add(() => IPAddress.TryParse(IpAddress, out _),
                "Not a valid IPv4 Address", nameof(IpAddress));
        }

        public override void Open()
        {
            base.Open();
            Connect();
        }

        private void Connect()
        {
            // Start monitoring serial port
            _logBuffer.Clear();
            _readBuffer.Clear();

            _client = new TelnetClient(IpAddress, TcpPort);
            _client.Logger = Logger;
            _client.Events.DataReceived += OnDataReceived;
            _client.Settings.NoDelay = true;

            // Number of milliseconds to wait when attempting to connect.
            _client.Settings.ConnectTimeoutMs = 5000;

            // Maximum amount of time to wait before considering the server to be idle and disconnecting from it.
            // By default, this value is set to 0, which will never disconnect due to inactivity.
            _client.Settings.IdleServerTimeoutMs = 0;

            _client.Connect();
        }

        public override void Close()
        {
            Disconnect();
            base.Close();
        }

        private void Disconnect()
        {
            // Stop monitoring serial port
            _client.Events.DataReceived -= OnDataReceived;
            _logBuffer.Clear();
            _readBuffer.Clear();

            // Close serial port
            _client.Disconnect();
        }

        private void Logger(string msg)
        {
            Log.Debug(msg);
        }

        private void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data.Array == null)
                return;

            var data = Encoding.UTF8.GetString(e.Data.Array, 0, e.Data.Count);
            _readBuffer.Append(data);
            ReadEvent?.Invoke(_readBuffer.ToString());
            LogLineByLine(data, e.IpPort);
        }

        public void Login(string expectedUsernamePrompt, string expectedPasswordPrompt, string expectedShellPrompt, int timeout)
        {
            throw new NotImplementedException();
        }

        public bool Expect(string expectedResponse, int timeout)
        {
            // Start monitoring serial port
            _readBuffer.Clear();
            _response = string.Empty;
            _responseReceived = false;
            _expectedResponse = expectedResponse;
            ReadEvent += OnReadEvent;

            // Wait for serial port to receive expected response
            var waitEnded = _waitForEvent.WaitOne(timeout * 1000);
            if (!waitEnded)
                Log.Warning("Serial port timed-out!");

            // Stop monitoring serial port
            ReadEvent -= OnReadEvent;
            _waitForEvent.Reset();

            return _responseReceived && waitEnded;
        }

        private void OnReadEvent(string readBuffer)
        {
            if (readBuffer.Contains(_expectedResponse))
            {
                _response = readBuffer;
                _responseReceived = true;
                _readBuffer.Clear();
                _waitForEvent.Set();
            }
        }

        public string Query(string command, string expectedEndOfMessage, int timeout)
        {
            _readBuffer.Clear();
            Write(command);
            Expect(expectedEndOfMessage, timeout);
            var response = _response;

            // ToDo: remove debug lines below
            // var msg = Regex.Replace(response, @"\t|\n|\r", "_");
            // Log.Debug($"RESPONSE: {msg}");

            return response;
        }

        public void Write(string command)
        {
            // ToDo: remove debug line below
            // Log.Debug($"{_client.ServerAddress} >> {command}");

            _client.Send(command + "\n");
        }

        private void LogLineByLine(string data, string tag)
        {
            foreach (var c in data)
            {
                _logBuffer.Append(c);

                // Show one line per log message
                if (c != '\n' && c != '\r')
                    continue;

                var currentLine = _logBuffer.ToString();
                _logBuffer.Clear();

                // Split into lines
                var lines = currentLine.Split(new[] { "\r\n", "\n\r", "\r", "\n" },
                    StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    // Go to the next foreach line, if string is empty
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Remove ANSI escape codes from log message
                    var lineWithoutAnsiEscapeCodes =
                        Regex.Replace(line, @"\x1B\[[^@-~]*[@-~]", "", RegexOptions.Compiled);

                    // Go to the next foreach line, if string is empty
                    if (string.IsNullOrWhiteSpace(lineWithoutAnsiEscapeCodes))
                        continue;

                    var msg = $"{tag} << {lineWithoutAnsiEscapeCodes}";

                    // Truncate log message to a maximum sting length
                    const int maxLength = 500;
                    if (msg.Length > maxLength)
                        msg = msg.Substring(0, maxLength) + "***";

                    Log.Debug(msg);
                }
            }
        }
    }
}