// Rakinda LV3000U and LV3000H Barcode Scanners
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
using System.Reflection;
using System.Text;
using OpenTap;
using TapExtensions.Interfaces.BarcodeScanner;
using TapExtensions.Shared.SystemManagement;

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
        public bool VerboseLoggingEnabled { get; set; } = false;

        #endregion

        private const int Timeout = 5;
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

        private bool ValidateConnectionAddress()
        {
            return UsbSerialDevices.ValidateConnectionAddress(ConnectionAddress);
        }

        public override void Open()
        {
            FindSerialPort();
            CheckIfBarcodeScannerIsAvailable();
        }

        private void FindSerialPort()
        {
            if (string.IsNullOrWhiteSpace(ConnectionAddress))
                throw new InvalidOperationException(
                    $"{nameof(ConnectionAddress)} cannot be empty");

            if (VerboseLoggingEnabled)
                Log.Debug($"Searching for USB Address(es) of '{ConnectionAddress}'");

            var found = UsbSerialDevices.FindUsbSerialDevice(ConnectionAddress);

            Log.Debug($"Found serial port '{found.ComPort}' " +
                      $"at USB Address of '{found.UsbAddress}' " +
                      $"with Description of '{found.Description}'");

            _portName = found.ComPort;
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

        public override void Close()
        {
            CloseSerialPort();
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
                Wakeup();
                DefaultAllCommands();
                SetReadingModeToTrigger();
                EnableCommandProgramming();
                EnableAllBarcodes();
            }
            finally
            {
                CloseSerialPort();
            }
        }

        public byte[] GetRawBytes()
        {
            byte[] rawBarcodeLabel;

            OpenSerialPort();
            try
            {
                StartScanning();
                try
                {
                    // Attempt to read the barcode label
                    var expectedEndOfBarcodeLabel = new byte[] { 0x0D, 0x0A };
                    rawBarcodeLabel = SerialRead(expectedEndOfBarcodeLabel, Timeout);

                    // Always show barcode label characters
                    if (!VerboseLoggingEnabled && rawBarcodeLabel.Length > 0)
                        Log.Debug(AsciiBytesToString(rawBarcodeLabel));
                }
                finally
                {
                    StopScanning();
                }
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

        private void SerialQuery(byte[] command, byte[] expectedEndOfMessage, int timeout)
        {
            SerialWrite(command);
            SerialRead(expectedEndOfMessage, timeout);
        }

        private void SerialWrite(byte[] command)
        {
            if (command == null || command.Length <= 0)
                return;

            OnActivity();

            if (VerboseLoggingEnabled)
                Log.Debug("{0} >> {1}", _sp.PortName, AsciiBytesToString(command));

            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
            _sp.Write(command, 0, command.Length);
        }

        private byte[] SerialRead(byte[] expectedResponse, int timeout)
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

            if (VerboseLoggingEnabled && response.Count > 0)
                Log.Debug("{0} << {1}", _sp.PortName, AsciiBytesToString(response.ToArray()));

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

        private static string AsciiBytesToString(byte[] bytes)
        {
            var msg = new StringBuilder();
            if (bytes != null && bytes.Length != 0)
                foreach (var c in bytes)
                    if (c >= 0x20 && c <= 0x7E)
                        msg.Append((char)c);
                    else
                        msg.Append("{" + c.ToString("X2") + "}");

            return msg.ToString();
        }

        private void SetCommand(string command, int timeout)
        {
            // When received a set command, the scanner would process it and returned a byte of response data.
            // The scanner returns '0x06' if successfully set, or '0x15' if failure.
            var cmdBytes = Encoding.ASCII.GetBytes(command);
            var expectedSuccessfulReply = new byte[] { 0x06 };
            SerialQuery(cmdBytes, expectedSuccessfulReply, timeout);
        }

        #region Rakinda's Commands

        private void Wakeup()
        {
            // Send "?" and expect the response to be "!"
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            SerialQuery(new byte[] { 0x3F }, new byte[] { 0x21 }, Timeout);
        }

        private void DefaultAllCommands()
        {
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            SetCommand("NLS0001000;", Timeout);
        }

        private void SetReadingModeToTrigger()
        {
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            SetCommand("NLS0302000;", Timeout);
        }

        private void EnableCommandProgramming()
        {
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            SetCommand("NLS0006010;", Timeout);
        }

        private void EnableAllBarcodes()
        {
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            SetCommand("NLS0001020;", Timeout);
        }

        private void StartScanning()
        {
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            SerialQuery(new byte[] { 0x1B, 0x31 }, new byte[] { 0x06 }, Timeout);
        }

        private void StopScanning()
        {
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            SerialQuery(new byte[] { 0x1B, 0x30 }, new byte[] { 0x06 }, Timeout);
        }

        #endregion
    }
}