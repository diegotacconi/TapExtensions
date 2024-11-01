using System;
using System.IO.Ports;
using OpenTap;
using TapExtensions.Shared.Win32;

namespace TapExtensions.Instruments.SigGen
{
    public abstract class WindfreakBase : Instrument
    {
        #region Settings

        [Display("Connection Address", Order: 1,
            Description: "Examples:\n" +
                         " USB\\VID_16D0&PID_0557\\004571\n" +
                         " USB\\VID_16D0&PID_0557\n" +
                         " COM5")]
        public string ConnectionAddress { get; set; }

        [Display("Verbose Logging", Order: 20,
            Description: "Enables verbose logging of serial port (UART) communication.")]
        public bool VerboseLoggingEnabled { get; set; } = true;

        #endregion

        private SerialPort _sp;

        private protected bool ValidateConnectionAddress()
        {
            return UsbSerialDevices.ValidateConnectionAddress(ConnectionAddress);
        }

        public override void Open()
        {
            base.Open();
            IsConnected = false;

            var portName = FindSerialPort();
            OpenSerialPort(portName);
        }

        private string FindSerialPort()
        {
            if (string.IsNullOrWhiteSpace(ConnectionAddress))
                throw new InvalidOperationException(
                    $"{nameof(ConnectionAddress)} cannot be empty");

            if (VerboseLoggingEnabled)
                Log.Debug($"Searching for USB Address(es) of '{ConnectionAddress}'");

            var found = UsbSerialDevices.FindUsbSerialDevice(ConnectionAddress);

            Log.Debug($"Found serial port '{found.ComPort}' " +
                      $"with USB Address of '{found.UsbAddress}' " +
                      $"and Description of '{found.Description}'");

            return found.ComPort;
        }

        private void OpenSerialPort(string portName)
        {
            if (string.IsNullOrWhiteSpace(portName))
                throw new InvalidOperationException(
                    "Serial Port Name cannot be empty");

            _sp = new SerialPort
            {
                PortName = portName,
                BaudRate = 9600, // Not applicable
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.RequestToSend,
                ReadTimeout = 2000,
                WriteTimeout = 2000,
                DtrEnable = true,
                RtsEnable = true
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
            base.Close();
            IsConnected = false;
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

        private protected virtual string SerialQuery(string command)
        {
            SerialWrite(command);
            return SerialRead();
        }

        private protected virtual void SerialWrite(string command)
        {
            if (string.IsNullOrEmpty(command))
                return;

            OnActivity();

            if (VerboseLoggingEnabled)
                Log.Debug("{0} >> {1}", _sp.PortName, command);

            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
            _sp.Write(command);
        }

        private protected virtual string SerialRead()
        {
            OnActivity();
            var response = string.Empty;
            const int timeoutMs = 3000;
            const int intervalMs = 10;
            const int maxCount = timeoutMs / intervalMs;
            var loopCount = 0;
            do
            {
                loopCount++;
                response += _sp.ReadExisting();
                TapThread.Sleep(intervalMs);
            } while (!response.Contains("\n") && loopCount < maxCount);

            if (VerboseLoggingEnabled && !string.IsNullOrEmpty(response))
                Log.Debug("{0} << {1}", _sp.PortName, response.Trim('\n'));

            return response;
        }
    }
}