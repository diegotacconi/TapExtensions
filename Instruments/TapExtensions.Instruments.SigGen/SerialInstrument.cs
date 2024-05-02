using System;
using System.Collections.Generic;
using System.IO.Ports;
using OpenTap;
using TapExtensions.Shared.Win32;

namespace TapExtensions.Instruments.SigGen
{
    public abstract class SerialInstrument : Instrument
    {
        #region Settings

        [EnabledIf(nameof(UseAutoDetection), false)]
        [Display("Serial Port Name", Order: 1, Description: "Example: 'COM1'")]
        public string SerialPortName { get; set; }

        [Display("Use AutoDetection", Order: 2, Group: "Serial Port AutoDetection", Collapsed: true)]
        public bool UseAutoDetection { get; set; } = false;

        [EnabledIf(nameof(UseAutoDetection))]
        [Display("USB Device Address", Order: 3, Group: "Serial Port AutoDetection", Collapsed: true,
            Description: "List of USB device addresses to search for a match.\n" +
                         @"Example: 'USB\VID_1234&PID_5678\SERIALNUMBER'")]
        public List<string> UsbDeviceAddresses { get; set; }

        #endregion

        private SerialPort _sp;

        public override void Open()
        {
            base.Open();
            var portName = UseAutoDetection ? FindSerialPort() : SerialPortName;
            OpenSerialPort(portName);
        }

        private string FindSerialPort()
        {
            if (UsbDeviceAddresses.Count == 0)
                throw new InvalidOperationException(
                    "List of USB Device Address cannot be empty");

            Log.Debug("Searching for USB Instance Path(s) of " +
                      $"'{string.Join("', '", UsbDeviceAddresses)}'");

            var found = UsbDevices.FindInstancePath(UsbDeviceAddresses);

            Log.Debug($"Found serial port '{found.ComPort}' " +
                      $"with USB Instance Path of '{found.InstancePath}' " +
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
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.RequestToSend,
                ReadTimeout = 2000, // 2 second
                WriteTimeout = 2000, // 2 second
                DtrEnable = true,
                RtsEnable = true
            };

            // Close serial port if already opened
            CloseSerialPort();

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
    }
}