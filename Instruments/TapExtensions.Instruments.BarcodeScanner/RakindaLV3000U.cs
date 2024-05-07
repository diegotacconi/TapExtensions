// Information on the Rakinda LV3000U and LV3000H barcode scanners:
// https://www.rakinda.com/en/productdetail/83/118/154.html
// https://www.rakinda.com/en/productdetail/83/135/95.html
// https://rakindaiot.com/product/mini-barcode-scanner-lv3000u-2d-with-external-insulation-board/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using OpenTap;
using TapExtensions.Shared.Win32;

namespace TapExtensions.Instruments.BarcodeScanner
{
    [Display("Rakinda LV3000U",
        Groups: new[] { "TapExtensions", "Instruments", "BarcodeScanner" },
        Description: "Rakinda LV3000U or LV3000H Fixed Mount Scanner")]
    public class RakindaLV3000U : BarcodeScannerBase
    {
        #region Settings

        // https://github.com/diegotacconi/TapExtensions/tree/main/Instruments/TapExtensions.Instruments.BarcodeScanner/ConfigDocs
        [EnabledIf(nameof(UseAutoDetection), false)]
        [Display("Serial Port Name", Order: 1,
            Description: "Remember to configure the scanner as a serial port (UART) device, over USB.")]
        public string SerialPortName { get; set; }

        [Display("Use AutoDetection", Order: 2, Group: "Serial Port AutoDetection", Collapsed: true)]
        public bool UseAutoDetection { get; set; }

        [EnabledIf(nameof(UseAutoDetection))]
        [Display("USB Device Address", Order: 3, Group: "Serial Port AutoDetection", Collapsed: true,
            Description: "List of USB device addresses to search for a match.")]
        public List<string> UsbDeviceAddresses { get; set; }

        public enum ELoggingLevel
        {
            None = 0,
            Normal = 1,
            Verbose = 2
        }

        [Display("Logging Level", Order: 20, Group: "Debug", Collapsed: true,
            Description: "Level of verbose logging for serial port (UART) communication.")]
        public ELoggingLevel LoggingLevel { get; set; }

        #endregion

        private string _portName;
        private SerialPort _sp;

        public RakindaLV3000U()
        {
            // Default values
            Name = nameof(RakindaLV3000U);
            SerialPortName = "COM6";
            UseAutoDetection = true;
            UsbDeviceAddresses = new List<string> { @"USB\VID_1EAB&PID_1D06" };
            LoggingLevel = ELoggingLevel.Normal;
        }

        public override void Open()
        {
            base.Open();

            _portName = UseAutoDetection ? FindSerialPort() : SerialPortName;

            // Check if barcode scanner is available
            OpenSerialPort();
            try
            {
                const int timeout = 5;

                // Send "?" and expect the response to be "!"
                WriteRead(new byte[] { 0x3F }, new byte[] { 0x21 }, timeout);

                // Default all commands
                SetCommand("NLS0001000;", timeout);

                // Reading mode = Trigger
                SetCommand("NLS0302000;", timeout);

                // Enable command programming
                SetCommand("NLS0006010;", timeout);

                // Enable all bar codes
                SetCommand("NLS0001020;", timeout);
            }
            finally
            {
                CloseSerialPort();
            }
        }

        private void SetCommand(string command, int timeout)
        {
            // When received a set command, the scanner would process it and returned a byte of response data.
            // The scanner returns '0x06' if successfully set, or '0x15' if failure.
            var cmdBytes = Encoding.ASCII.GetBytes(command);
            var expectedSuccessfulReply = new byte[] { 0x06 };
            WriteRead(cmdBytes, expectedSuccessfulReply, timeout);
        }

        private string FindSerialPort()
        {
            if (UsbDeviceAddresses.Count == 0)
                throw new InvalidOperationException(
                    "List of USB Device Address cannot be empty");

            if (LoggingLevel >= ELoggingLevel.Verbose)
                Log.Debug("Searching for USB Address(es) of " +
                          $"'{string.Join("', '", UsbDeviceAddresses)}'");

            var found = UsbSerialDevices.FindUsbAddress(UsbDeviceAddresses);

            if (LoggingLevel >= ELoggingLevel.Normal)
                Log.Debug($"Found serial port '{found.ComPort}' " +
                          $"with USB Address of '{found.UsbAddress}' " +
                          $"and Description of '{found.Description}'");

            return found.ComPort;
        }

        private void OpenSerialPort()
        {
            if (string.IsNullOrWhiteSpace(_portName))
                throw new InvalidOperationException(
                    "Serial Port Name cannot be empty");

            _sp = new SerialPort
            {
                PortName = _portName,
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 1000, // 1 second
                WriteTimeout = 1000 // 1 second
            };

            // Close serial port if already opened
            CloseSerialPort();

            if (LoggingLevel >= ELoggingLevel.Normal)
                Log.Debug($"Opening serial port ({_sp.PortName})");

            // Open serial port
            _sp.Open();
            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
        }

        public override void Close()
        {
            CloseSerialPort();
            base.Close();
        }

        private void CloseSerialPort()
        {
            try
            {
                if (_sp.IsOpen)
                {
                    if (LoggingLevel >= ELoggingLevel.Normal)
                        Log.Debug($"Closing serial port ({_sp.PortName})");

                    // Close serial port
                    _sp.DiscardInBuffer();
                    _sp.DiscardOutBuffer();
                    _sp.Close();
                    _sp.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex.Message);
            }
        }

        public override byte[] GetRawBytes()
        {
            const int timeout = 5;
            byte[] rawBarcodeLabel;

            OpenSerialPort();
            try
            {
                // Start Scanning
                WriteRead(new byte[] { 0x1B, 0x31 }, new byte[] { 0x06 }, timeout);

                // Attempt to read the barcode label
                var expectedEndOfBarcodeLabel = new byte[] { 0x0D, 0x0A };
                rawBarcodeLabel = Read(expectedEndOfBarcodeLabel, timeout);

                // Stop Scanning
                WriteRead(new byte[] { 0x1B, 0x30 }, new byte[] { 0x06 }, timeout);
            }
            finally
            {
                CloseSerialPort();
            }

            return rawBarcodeLabel;
        }

        private void WriteRead(byte[] command, byte[] expectedEndOfMessage, int timeout)
        {
            Write(command);
            Read(expectedEndOfMessage, timeout);
        }

        private void Write(byte[] command)
        {
            LogBytes(_sp.PortName, ">>", command);
            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
            _sp.Write(command, 0, command.Length);
        }

        private byte[] Read(byte[] expectedResponse, int timeout)
        {
            bool responseReceived;
            var response = new List<byte>();
            var timer = new Stopwatch();
            timer.Start();

            do
            {
                TapThread.Sleep(10);
                var count = _sp.BytesToRead;
                var buffer = new byte[count];
                _sp.Read(buffer, 0, count);
                response.AddRange(buffer.ToList());
                responseReceived = FindPattern(response.ToArray(), expectedResponse) >= 0;

                if (timer.Elapsed > TimeSpan.FromSeconds(timeout))
                {
                    Log.Warning("Serial port timed-out!");
                    break;
                }
            } while (!responseReceived);

            timer.Stop();
            LogBytes(_sp.PortName, "<<", response.ToArray());

            if (!responseReceived)
                throw new InvalidOperationException("Did not receive the expected end of message");

            return response.ToArray();
        }

        private static int FindPattern(byte[] source, byte[] pattern)
        {
            var j = -1;
            for (var i = 0; i < source.Length; i++)
                if (source.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                    j = i;

            return j;
        }

        private void LogBytes(string serialPortName, string direction, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return;

            var msg = new StringBuilder();
            var hex = new StringBuilder();
            var ascii = new StringBuilder();

            foreach (var c in bytes)
            {
                hex.Append(c.ToString("X2") + " ");

                var j = c;
                if (j >= 0x20 && j <= 0x7E)
                {
                    msg.Append((char)j);
                    ascii.Append((char)j + "  ");
                }
                else
                {
                    msg.Append("{" + c.ToString("X2") + "}");
                    ascii.Append('.' + "  ");
                }
            }

            switch (LoggingLevel)
            {
                case ELoggingLevel.Normal:
                    Log.Debug($"{serialPortName} {direction} {msg}");
                    break;

                case ELoggingLevel.Verbose:
                    Log.Debug($"{serialPortName} {direction} Hex:   {hex}");
                    Log.Debug($"{serialPortName} {direction} Ascii: {ascii}");
                    break;
            }
        }
    }
}