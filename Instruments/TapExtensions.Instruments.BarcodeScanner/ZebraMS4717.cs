// Information on the Zebra MS4717 barcode scanner:
// https://www.zebra.com/us/en/products/oem/fixed-mount/ms4700-series.html
// https://www.zebra.com/content/dam/zebra_new_ia/en-us/manuals/oem/ms4717-ig-en.pdf
//
// This instrument driver implements parts of the Zebra's Simple Serial Interface (SSI),
// which enables barcode scanners to communicate with a host over a serial port (UART).
// https://www.google.com/search?q=zebra+simple+serial+interface+programmer+guide

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using OpenTap;

namespace TapExtensions.Instruments.BarcodeScanner
{
    [Display("Zebra MS4717",
        Groups: new[] { "TapExtensions", "Instruments", "BarcodeScanner" },
        Description: "Zebra MS4717 Fixed Mount Imager")]
    public class ZebraMS4717 : BarcodeScannerBase
    {
        #region Settings

        [Display("Serial Port Name", Order: 1,
            Description: "Remember to configure the scanner as a serial port (UART) device, over USB.")]
        [HelpLink(
            @"https://github.com/diegotacconi/TapExtensions/tree/main/Instruments/TapExtensions.Instruments.BarcodeScanner/ConfigDocs")]
        public string SerialPortName { get; set; }

        public enum ELoggingLevel
        {
            Verbose,
            Normal,
            None
        }

        [Display("Logging Level", Order: 2,
            Description: "Level of verbose logging for serial port (UART) communication.")]
        public ELoggingLevel LoggingLevel { get; set; }

        #endregion

        private SerialPort _sp;

        public ZebraMS4717()
        {
            // Default values
            Name = nameof(ZebraMS4717);
            SerialPortName = "COM3";
            LoggingLevel = ELoggingLevel.Normal;
        }

        public override void Open()
        {
            base.Open();

            // Check if barcode scanner is available
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

        private void OpenSerialPort()
        {
            // Example: USB\VID_05E0&PID_1701

            _sp = new SerialPort
            {
                PortName = SerialPortName,
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.RequestToSend,
                ReadTimeout = 1000, // 1 second
                WriteTimeout = 1000 // 1 second
            };

            // Close serial port if already opened
            CloseSerialPort();

            if (LoggingLevel == ELoggingLevel.Verbose || LoggingLevel == ELoggingLevel.Normal)
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
                    if (LoggingLevel == ELoggingLevel.Verbose || LoggingLevel == ELoggingLevel.Normal)
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
                Wakeup();
                ScanEnable();
                StartSession();

                // Attempt to read the barcode label
                rawBarcodeLabel = Read(new byte[0], timeout);

                StopSession();
                ScanDisable();
                Sleep();
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
            WriteRead(message.ToArray(), new[] { expectedByte }, timeout);
        }

        private byte[] CalculateChecksum(byte[] bytes)
        {
            // Twos complement of the sum of the message
            int sum = 0;
            foreach (var item in bytes)
                sum += (int)item;

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
            Query(0xC4, CmdAck);
        }

        private void AimOn()
        {
            // Activate aim pattern
            Query(0xC5, CmdAck);
        }

        private void Beep(byte beepCode)
        {
            // Sound the beeper
            Query(0xE6, new[] { beepCode }, CmdAck);
        }

        private void LedOff()
        {
            // Turn off the specified decoder LEDs
            Query(0xE8, new byte[] { 0x00 }, CmdAck);
        }

        private void LedOn()
        {
            // Turn on the specified decoder LEDs
            Query(0xE7, new byte[] { 0x00 }, CmdAck);
        }

        private void ParamDefaults()
        {
            // Set all parameters to their default values
            Query(0xC8, CmdAck);
        }

        private void ParamRequest(byte parameterNumber = 0xFE)
        {
            // Request values of certain parameters
            Query(0xC7, new[] { parameterNumber }, CmdAck);
        }

        private void ScanDisable()
        {
            // Prevents the operator from scanning bar codes
            Query(0xEA, CmdAck);
        }

        private void ScanEnable()
        {
            // Permits bar code scanning
            Query(0xE9, CmdAck);
        }

        private void Sleep()
        {
            // Requests to place the decoder into low power
            Query(0xEB, CmdAck);
        }

        private void StartSession()
        {
            // Tells the decoder to start a scan session
            Query(0xE4, CmdAck);
        }

        private void StopSession()
        {
            // Tells the decoder to abort a decode attempt or video transmission
            Query(0xE5, CmdAck);
        }

        private void Wakeup()
        {
            Write(new byte[] { 0x00 });
            TapThread.Sleep(100);
        }

        #endregion
    }
}