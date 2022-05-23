using System;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using OpenTap;
using TapExtensions.Interfaces.Uart;

namespace TapExtensions.Duts.Uart
{
    [Display("UartDut",
        Groups: new[] { "TapExtensions", "Duts", "Uart" })]
    public class UartDut : Dut, IUart
    {
        #region Settings

        [Display("Port Name", Order: 1, Group: "Serial Port Settings")]
        public string PortName { get; set; }

        [Display("Baud Rate", Order: 2, Group: "Serial Port Settings")]
        public int BaudRate { get; set; }

        [Display("Parity", Order: 3, Group: "Serial Port Settings")]
        public Parity Parity { get; set; }

        [Display("Data Bits", Order: 4, Group: "Serial Port Settings")]
        public int DataBits { get; set; }

        [Display("Stop Bits", Order: 5, Group: "Serial Port Settings")]
        public StopBits StopBits { get; set; }

        [Display("Flow Control", Order: 6, Group: "Serial Port Settings")]
        public Handshake Handshake { get; set; }

        [Display("Verbose Logging", Order: 7, Group: "Debug", Collapsed: true,
            Description: "Enables verbose logging of serial port (UART) communication.")]
        public bool VerboseLoggingEnabled { get; set; } = true;

        #endregion

        private delegate void UartEvent(string readBuffer);

        private event UartEvent ReadEvent;
        private SerialPort _sp;
        private readonly StringBuilder _logBuffer = new StringBuilder();
        private readonly StringBuilder _readBuffer = new StringBuilder();
        private static bool _responseReceived;
        private static string _expectedResponse;
        private readonly ManualResetEvent _waitForEvent = new ManualResetEvent(false);
        private string _response;

        public UartDut()
        {
            // Default values
            Name = nameof(UartDut);
            PortName = "COM13";
            BaudRate = 115200;
            Parity = Parity.None;
            DataBits = 8;
            StopBits = StopBits.One;
            Handshake = Handshake.None;
        }

        public override void Open()
        {
            base.Open();
            Connect();
        }

        private void Connect()
        {
            _sp = new SerialPort
            {
                PortName = PortName,
                BaudRate = BaudRate,
                Parity = Parity,
                DataBits = DataBits,
                StopBits = StopBits,
                Handshake = Handshake
            };

            // Close serial port if already opened
            if (_sp.IsOpen)
            {
                Log.Warning($"Closing serial port ({_sp.PortName})");
                _sp.DiscardInBuffer();
                _sp.DiscardOutBuffer();
                _sp.Close();
                _sp.Dispose();
            }

            if (VerboseLoggingEnabled)
                Log.Debug($"Opening serial port ({_sp.PortName}) with BaudRate={_sp.BaudRate}, " +
                          $"Parity={_sp.Parity}, DataBits={_sp.DataBits}, StopBits={_sp.StopBits}, " +
                          $"Handshake={_sp.Handshake}");
            else
                Log.Debug($"Opening serial port ({_sp.PortName})");

            // Open serial port
            _sp.Open();
            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();

            // Start monitoring serial port
            _logBuffer.Clear();
            _readBuffer.Clear();
            _sp.DataReceived += OnDataReceived;
        }

        public override void Close()
        {
            Disconnect();
            base.Close();
        }

        private void Disconnect()
        {
            Log.Debug($"Closing serial port ({_sp.PortName})");

            // Stop monitoring serial port
            _sp.DataReceived -= OnDataReceived;
            _logBuffer.Clear();
            _readBuffer.Clear();

            // Close serial port
            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
            _sp.Close();
            _sp.Dispose();
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var serialPort = (SerialPort)sender;
            var data = serialPort.ReadExisting();

            _readBuffer.Append(data);
            ReadEvent?.Invoke(_readBuffer.ToString());

            LogLineByLine(data);
        }

        private void LogLineByLine(string data)
        {
            foreach (var c in data)
            {
                _logBuffer.Append(c);

                // Show one line per log message
                if (c == '\n' || c == '\r')
                {
                    var currentLine = _logBuffer.ToString();
                    _logBuffer.Clear();

                    if (VerboseLoggingEnabled)
                    {
                        var lines = currentLine.Split(new[] { "\r\n", "\n\r", "\r", "\n" },
                            StringSplitOptions.RemoveEmptyEntries);

                        foreach (var line in lines)
                        {
                            // Remove ANSI escape codes from log message
                            var lineWithoutAnsiEscapeCodes =
                                Regex.Replace(line, @"\x1B\[[^@-~]*[@-~]", "", RegexOptions.Compiled);

                            var msg = $"{_sp.PortName} << {lineWithoutAnsiEscapeCodes}";

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

        public bool Expect(string expectedResponse, int timeout)
        {
            // Start monitoring serial port
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
            // var msg = Regex.Replace(response, @"\t|\n|\r", "_");
            // Log.Debug($"RESPONSE: {msg}");
            return response;
        }

        public void Write(string command)
        {
            // if (VerboseLoggingEnabled)
            //     Log.Debug($"{_sp.PortName} >> {command}");
            _sp.DiscardOutBuffer();
            _sp.WriteLine(command);
        }
    }
}