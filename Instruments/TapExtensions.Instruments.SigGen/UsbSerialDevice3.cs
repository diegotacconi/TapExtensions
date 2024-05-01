using System;
using System.Collections.Generic;
using System.IO.Ports;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.SigGen;
using TapExtensions.Shared.Win32;

namespace TapExtensions.Instruments.SigGen
{
    [Display("UsbSerialDevice3",
        Groups: new[] { "TapExtensions", "Instruments", "SigGen" })]
    public class UsbSerialDevice3 : Instrument, ISigGen
    {
        #region Settings

        [EnabledIf(nameof(UseAutoDetection), false)]
        [Display("Serial Port Name", Order: 1)]
        public string SerialPortName { get; set; }

        [Display("Use AutoDetection", Order: 2, Group: "Serial Port AutoDetection", Collapsed: true)]
        public bool UseAutoDetection { get; set; } = true;

        [EnabledIf(nameof(UseAutoDetection))]
        [Display("USB Device Address", Order: 3, Group: "Serial Port AutoDetection", Collapsed: true,
            Description: "List of USB device addresses to search for a match")]
        public List<string> UsbDeviceAddresses { get; set; }

        #endregion

        private SerialPort _sp;

        public UsbSerialDevice3()
        {
            // Default values
            Name = nameof(UsbSerialDevice3);
            SerialPortName = "COM8";
            UsbDeviceAddresses = new List<string>
            {
                @"USB\VID_10C4&PID_EA70&MI_01\A&6C616C5&0&0001"
            };
        }

        public override void Open()
        {
            base.Open();
            var portName = UseAutoDetection ? SearchForUsbDevice() : SerialPortName;
            OpenSerialPort(portName);
        }

        private string SearchForUsbDevice()
        {
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

        public double GetFrequency()
        {
            throw new NotImplementedException();
        }

        public double GetOutputLevel()
        {
            throw new NotImplementedException();
        }

        public EState GetRfOutputState()
        {
            throw new NotImplementedException();
        }

        public void SetFrequency(double frequencyMhz)
        {
            Log.Warning("SetFrequency() Not Implemented");
        }

        public void SetOutputLevel(double outputLevelDbm)
        {
            throw new NotImplementedException();
        }

        public void SetRfOutputState(EState state)
        {
            Log.Warning("SetRfOutputState() Not Implemented");
        }
    }
}