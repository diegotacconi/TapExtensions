// Zebra MS4717 Barcode Scanner
// https://www.zebra.com/us/en/products/oem/fixed-mount/ms4700-series.html
// https://www.zebra.com/content/dam/zebra_new_ia/en-us/manuals/oem/ms4717-ig-en.pdf
//
// This instrument driver implements parts of the Zebra's Simple Serial Interface (SSI),
// which enables barcode scanners to communicate with a host over a serial port (UART).
// https://www.google.com/search?q=zebra+simple+serial+interface+programmer+guide
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
    [Display("Zebra MS4717",
        Groups: new[] { "TapExtensions", "Instruments", "BarcodeScanner" },
        Description: "Zebra MS4717 Fixed Mount Imager")]
    public class ZebraMS4717 : Instrument, IBarcodeScanner
    {
        #region Settings

        [Display("Connection Address", Order: 1,
            Description: "Examples:\n" +
                         " USB\\VID_05E0&PID_1701\\USB_CDC_SYMBOL_SCANNER\n" +
                         " USB\\VID_05E0&PID_1701\n" +
                         " COM4")]
        public string ConnectionAddress { get; set; }

        [Display("Verbose Logging", Order: 20,
            Description: "Enables verbose logging of serial port (UART) communication.")]
        public bool VerboseLoggingEnabled { get; set; } = false;

        #endregion

        private const int Timeout = 5;
        private string _portName;
        private SerialPort _sp;

        public ZebraMS4717()
        {
            // Default values
            Name = nameof(ZebraMS4717);
            ConnectionAddress = @"USB\VID_05E0&PID_1701";

            // Validation rules
            Rules.Add(ValidateConnectionAddress, "Not valid", nameof(ConnectionAddress));
        }

        private bool ValidateConnectionAddress()
        {
            return UsbSerialDevices.ValidateAddress(ConnectionAddress);
        }

        public override void Open()
        {
            FindSerialPort();
            CheckIfBarcodeScannerIsAvailable();
        }

        private void FindSerialPort()
        {
            /*
            // List USB devices
            if (VerboseLoggingEnabled)
            {
                var devices = UsbSerialDevices.FindAllDevices();
                foreach (var device in devices)
                    Log.Debug($"'{device.ComPort}', '{device.UsbAddress}', '{device.Description}'.");
            }
            */

            if (string.IsNullOrWhiteSpace(ConnectionAddress))
                throw new InvalidOperationException(
                    $"{nameof(ConnectionAddress)} cannot be empty");

            if (VerboseLoggingEnabled)
                Log.Debug($"Searching for USB Address(es) of '{ConnectionAddress}'");

            var found = UsbSerialDevices.FindDevice(ConnectionAddress);

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
                Handshake = Handshake.RequestToSend,
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
                ParamDefaults();
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
                Wakeup();
                ScanEnable();
                StartSession();
                try
                {
                    // Attempt to read the barcode label
                    var expectedEndOfBarcodeLabel = Array.Empty<byte>();
                    rawBarcodeLabel = SerialRead(expectedEndOfBarcodeLabel, Timeout);

                    // Always show barcode label characters
                    if (!VerboseLoggingEnabled && rawBarcodeLabel.Length > 0)
                        Log.Debug(AsciiBytesToString(rawBarcodeLabel));
                }
                finally
                {
                    StopSession();
                    ScanDisable();
                    Sleep();
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
            // Scan barcode label
            var rawBytes = GetRawBytes();

            // Parse barcode label
            var productCode = BarcodeLabelUtility.GetProductCode(rawBytes);
            var serialNumber = BarcodeLabelUtility.GetSerialNumber(rawBytes);

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

        private void Query(byte opCode, byte expectedByte)
        {
            Query(opCode, new byte[0], expectedByte);
        }

        private void Query(byte opCode, byte[] parameters, byte expectedByte, int timeout = 1)
        {
            // Packet Format: <Length> <Opcode> <Message Source> <Status> <Data....> <Checksum>

            var message = new List<byte>
            {
                opCode, // Opcode (1 Byte)
                0x04, // Message Source (1 Byte), 0x00 = Decoder, 0x04 = Host
                0x00 // Status (1 Byte), First time packet, Temporary change
            };

            // Add parameters, if any
            foreach (var parameter in parameters)
                message.Add(parameter);

            // Prepend length (1 Byte)
            // Length of message not including the check sum bytes. Maximum value is 0xFF.
            message.Insert(0, BitConverter.GetBytes(message.Count + 1)[0]);

            // Add checksum (2 Bytes)
            var checksum = CalculateChecksum(message.ToArray());
            message.Add(checksum[1]); // High byte
            message.Add(checksum[0]); // Low byte

            // Send message
            SerialQuery(message.ToArray(), new[] { expectedByte }, timeout);
        }

        private static byte[] CalculateChecksum(byte[] bytes)
        {
            // Twos complement of the sum of the message
            var sum = 0;
            foreach (var item in bytes)
                sum += item;

            var sum16 = (ushort)(sum % 255);
            var twosComplement = (ushort)(~sum16 + 1);
            var checksumBytes = BitConverter.GetBytes(twosComplement);
            return checksumBytes;
        }

        #region Zebra's SSI Commands

        private const byte CmdAck = 0xD0;
        private const byte CmdNak = 0xD1;

        private void AimOff()
        {
            // Deactivate aim pattern
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            Query(0xC4, CmdAck);
        }

        private void AimOn()
        {
            // Activate aim pattern
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            Query(0xC5, CmdAck);
        }

        private void Beep(byte beepCode)
        {
            // Sound the beeper
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            Query(0xE6, new[] { beepCode }, CmdAck);
        }

        private void LedOff()
        {
            // Turn off the specified decoder LEDs
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            Query(0xE8, new byte[] { 0x00 }, CmdAck);
        }

        private void LedOn()
        {
            // Turn on the specified decoder LEDs
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            Query(0xE7, new byte[] { 0x00 }, CmdAck);
        }

        private void ParamDefaults()
        {
            // Set all parameters to their default values
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            Query(0xC8, CmdAck);
        }

        private void ParamRequest(byte parameterNumber = 0xFE)
        {
            // Request values of certain parameters
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            Query(0xC7, new[] { parameterNumber }, CmdAck);
        }

        private void ScanDisable()
        {
            // Prevents the operator from scanning bar codes
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            Query(0xEA, CmdAck);
        }

        private void ScanEnable()
        {
            // Permits bar code scanning
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            Query(0xE9, CmdAck);
        }

        private void Sleep()
        {
            // Requests to place the decoder into low power
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            Query(0xEB, CmdAck);
        }

        private void StartSession()
        {
            // Tells the decoder to start a scan session
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            Query(0xE4, CmdAck);
        }

        private void StopSession()
        {
            // Tells the decoder to abort a decode attempt or video transmission
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            Query(0xE5, CmdAck);
        }

        private void Wakeup()
        {
            Log.Debug(MethodBase.GetCurrentMethod()?.Name);
            SerialWrite(new byte[] { 0x00 });
            TapThread.Sleep(100);
        }

        #endregion
    }
}