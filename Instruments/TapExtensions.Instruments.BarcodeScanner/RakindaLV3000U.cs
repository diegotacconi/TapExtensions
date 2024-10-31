// Information on the Rakinda LV3000U and LV3000H barcode scanners:
// https://www.rakinda.com/en/productdetail/83/118/154.html
// https://www.rakinda.com/en/productdetail/83/135/95.html
// https://rakindaiot.com/product/mini-barcode-scanner-lv3000u-2d-with-external-insulation-board/
//
// Remember to configure the scanner as a USB Serial Device, not as a USB Keyboard
// https://github.com/diegotacconi/TapExtensions/tree/main/Instruments/TapExtensions.Instruments.BarcodeScanner/ConfigDocs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenTap;
using TapExtensions.Interfaces.BarcodeScanner;
using TapExtensions.Shared.Win32;

namespace TapExtensions.Instruments.BarcodeScanner
{
    [Display("Rakinda LV3000U",
        Groups: new[] { "TapExtensions", "Instruments", "BarcodeScanner" },
        Description: "Rakinda LV3000U or LV3000H Fixed Mount Scanner")]
    public class RakindaLV3000U : Instrument, IBarcodeScanner
    {
        #region Settings

        [Display("Connection Address", Order: 1,
            Description: "Examples:\n" +
                         " USB\\VID_1EAB&PID_1D06\\CF078472\n" +
                         " USB\\VID_1EAB&PID_1D06\n" +
                         " COM3")]
        public string ConnectionAddress { get; set; }

        [Display("Retry", Order: 10,
            Description: "Maximum number of iteration attempts to retry scanning the barcode label.")]
        public Enabled<int> MaxIterationCount { get; set; }

        [Display("Verbose Logging", Order: 20,
            Description: "Enables verbose logging of serial port (UART) communication.")]
        public bool VerboseLoggingEnabled { get; set; } = true;

        #endregion

        private string _portName;
        private SerialPort _sp;

        public RakindaLV3000U()
        {
            // Default values
            Name = nameof(RakindaLV3000U);
            ConnectionAddress = @"USB\VID_1EAB&PID_1D06";
            MaxIterationCount = new Enabled<int> { IsEnabled = true, Value = 3 };

            // Validation rules
            Rules.Add(ValidateConnectionAddress, "Not valid", nameof(ConnectionAddress));
            Rules.Add(() => MaxIterationCount.Value > 0,
                "Must be greater than zero", nameof(MaxIterationCount));
        }

        public bool ValidateConnectionAddress()
        {
            // Split addresses string into multiple address strings
            var separators = new List<char> { ',', ';', '\t', '\n', '\r' };
            var parts = ConnectionAddress.Split(separators.ToArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

            // Remove all white-spaces from the beginning and end of the address string
            var addresses = parts.Select(part => part.Trim()).ToList();

            var validAddresses = new List<bool>();
            foreach (var address in addresses)
            {
                const string comPortPattern = "^[Cc][Oo][Mm][1-9][0-9]*$";
                const string usbDevicePattern = "^USB.*";
                var validAddress = Regex.IsMatch(address, comPortPattern) || Regex.IsMatch(address, usbDevicePattern);
                validAddresses.Add(validAddress);
            }

            return validAddresses.Any() && validAddresses.All(x => x);
        }

        public override void Open()
        {
            base.Open();
            IsConnected = false;

            FindSerialPort();
            CheckIfBarcodeScannerIsAvailable();
        }

        public override void Close()
        {
            CloseSerialPort();
            base.Close();
            IsConnected = false;
        }

        private void FindSerialPort()
        {
            if (string.IsNullOrWhiteSpace(ConnectionAddress))
                throw new InvalidOperationException($"{nameof(ConnectionAddress)} cannot be empty");

            if (VerboseLoggingEnabled)
                Log.Debug($"Searching for USB Address(es) of '{ConnectionAddress}'");

            var found = UsbSerialDevices.FindUsbSerialDevice(ConnectionAddress);
            _portName = found.ComPort;

            Log.Debug($"Found serial port '{found.ComPort}' " +
                      $"with USB Address of '{found.UsbAddress}' " +
                      $"and Description of '{found.Description}'");
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
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };

            // Close serial port if already opened
            CloseSerialPort();

            if (VerboseLoggingEnabled)
                Log.Debug($"Opening serial port ({_sp.PortName})");

            // Open serial port
            _sp.Open();
            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
            IsConnected = true;
        }

        private void CloseSerialPort()
        {
            if (!_sp.IsOpen)
                return;

            if (VerboseLoggingEnabled)
                Log.Debug($"Closing serial port ({_sp.PortName})");

            // Close serial port
            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
            _sp.Close();
            _sp.Dispose();
            IsConnected = false;
        }

        private void CheckIfBarcodeScannerIsAvailable()
        {
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

        public byte[] GetRawBytes()
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

        public (string serialNumber, string productCode) GetSerialNumberAndProductCode()
        {
            var serialNumber = "";
            var productCode = "";
            var maxCount = MaxIterationCount.IsEnabled ? MaxIterationCount.Value : 1;

            // Retry loop
            for (var iteration = 1; iteration <= maxCount; iteration++)
                try
                {
                    if (iteration > 1)
                        Log.Warning($"Retrying attempt {iteration} of {maxCount} ...");

                    // Try to scan the barcode label
                    var rawBytes = GetRawBytes();

                    // Parse the barcode label
                    productCode = BarcodeLabelUtility.GetProductCode(rawBytes);
                    serialNumber = BarcodeLabelUtility.GetSerialNumber(rawBytes);

                    // Exit loop if no exceptions
                    break;
                }
                catch (Exception ex)
                {
                    if (iteration < maxCount)
                        Log.Debug($"IgnoreException: {ex.Message}");
                    else
                        throw;
                }

            return (serialNumber, productCode);
        }

        private void WriteRead(byte[] command, byte[] expectedEndOfMessage, int timeout)
        {
            Write(command);
            Read(expectedEndOfMessage, timeout);
        }

        private void Write(byte[] command)
        {
            OnActivity();
            LogBytes(_sp.PortName, ">>", command);
            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
            _sp.Write(command, 0, command.Length);
        }

        private byte[] Read(byte[] expectedResponse, int timeout)
        {
            OnActivity();
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

            if (VerboseLoggingEnabled)
                Log.Debug($"{serialPortName} {direction} {msg}");
        }

        private void SetCommand(string command, int timeout)
        {
            // When received a set command, the scanner would process it and returned a byte of response data.
            // The scanner returns '0x06' if successfully set, or '0x15' if failure.
            var cmdBytes = Encoding.ASCII.GetBytes(command);
            var expectedSuccessfulReply = new byte[] { 0x06 };
            WriteRead(cmdBytes, expectedSuccessfulReply, timeout);
        }
    }
}