using System;
using System.IO.Ports;
using OpenTap;

namespace TapExtensions.Instruments.MultipleInterfaces.RasPi
{
    [Display("RasPiUart",
        Groups: new[] { "TapExtensions", "Instruments", "MultipleInterfaces" })]
    public class RasPiUart : Resource
    {
        #region Settings

        [Display("Serial Port Name", Order: 1)]
        public string SerialPortName { get; set; }

        [Display("Verbose Logging", Order: 100, Group: "Debug", Collapsed: true)]
        public bool VerboseLoggingEnabled { get; set; } = true;

        #endregion

        private SerialPort _sp;

        public RasPiUart()
        {
            // Default values
            Name = nameof(RasPiUart);
            SerialPortName = "COM5";
        }

        public override void Open()
        {
            base.Open();
            IsConnected = false;
        }

        public override void Close()
        {
            CloseSerialPort();
            base.Close();
            IsConnected = false;
        }

        private void OpenSerialPort()
        {
            if (string.IsNullOrWhiteSpace(SerialPortName))
                throw new InvalidOperationException(
                    "Serial Port Name cannot be empty");

            _sp = new SerialPort
            {
                PortName = SerialPortName,
                BaudRate = 115200,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.XOnXOff,
                ReadTimeout = 1000, // 1 second
                WriteTimeout = 1000 // 1 second
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
            try
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
            catch (Exception ex)
            {
                Log.Warning(ex.Message);
            }
        }
    }
}