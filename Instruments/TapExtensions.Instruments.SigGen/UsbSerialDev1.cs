using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.SigGen;
using TapExtensions.Shared.Win32;

namespace TapExtensions.Instruments.SigGen
{
    [Display("UsbSerialDev1",
        Groups: new[] { "TapExtensions", "Instruments", "SigGen" })]
    public class UsbSerialDev1 : Instrument, ISigGen
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

        /*
        [Browsable(true)]
        [Display("Show available USB Devices", Order: 5, Group: "Serial Port AutoDetection", Collapsed: true)]
        [EnabledIf(nameof(UseAutoDetection))]
        public void ShowAvailableUsbDevicesButton()
        {
            var devices = UsbDevices.GetAllSerialDevices();

            var msg = "";
            foreach (var device in devices)
                msg += $"'{device.ComPort}', '{device.InstancePath}', '{device.Description}'\n";

            UserInput.Request(new DialogWindow(msg), true);
        }

        [Browsable(true)]
        [Display("Try AutoDetection", Order: 6, Group: "Serial Port AutoDetection", Collapsed: true,
            Description: "Click this button to try to find a \nUSB device that matches the search criteria")]
        [EnabledIf(nameof(UseAutoDetection))]
        public void TryAutoDetectionButton()
        {
            var found = UsbDevices.FindInstancePath(UsbDeviceAddresses);

            var msg = $"Found serial port at '{found.ComPort}' " +
                      $"with USB Instance Path of '{found.InstancePath}' " +
                      $"and Description of '{found.Description}'";

            UserInput.Request(new DialogWindow(msg), true);
        }
        */

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

        private SerialPort _sp;

        public UsbSerialDev1()
        {
            // Default values
            Name = nameof(UsbSerialDev1);
            SerialPortName = "COM6";

            UsbDeviceAddresses = new List<string>
            {
                @"USB\VID_16D0&PID_0557\004571",
                @"USB\VID_16D0&PID_0557"
            };

            LoggingLevel = ELoggingLevel.Normal;
        }

        public override void Open()
        {
            base.Open();
            var portName = UseAutoDetection ? SearchForUsbDevice() : SerialPortName;
            OpenSerialPort(portName);
        }

        private void ShowAvailableUsbDevices()
        {
            var devices = UsbDevices.GetAllSerialDevices();
            foreach (var device in devices)
                Log.Debug($"'{device.ComPort}', '{device.InstancePath}', '{device.Description}'");
        }

        private string SearchForUsbDevice()
        {
            if (LoggingLevel >= ELoggingLevel.Verbose)
                Log.Debug("Searching for USB Instance Path(s) of " +
                          $"'{string.Join("', '", UsbDeviceAddresses)}'");

            var found = UsbDevices.FindInstancePath(UsbDeviceAddresses);

            if (LoggingLevel >= ELoggingLevel.Normal)
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

            switch (LoggingLevel)
            {
                case ELoggingLevel.Normal:
                    Log.Debug($"Opening serial port ({_sp.PortName})");
                    break;

                case ELoggingLevel.Verbose:
                    Log.Debug($"Opening serial port ({_sp.PortName}) with BaudRate={_sp.BaudRate}, " +
                              $"Parity={_sp.Parity}, DataBits={_sp.DataBits}, StopBits={_sp.StopBits}, " +
                              $"Handshake={_sp.Handshake}");
                    break;
            }

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

    [Display("Auto-Detection Dialog")]
    internal class DialogWindow
    {
        public DialogWindow(string message)
        {
            Message = message;
        }

        [Browsable(true)]
        [Layout(LayoutMode.FullRow, 2)]
        public string Message { get; }

        [Layout(LayoutMode.FloatBottom | LayoutMode.FullRow)]
        [Submit]
        public EDialogButton Response { get; set; } = EDialogButton.Ok;
    }

    internal enum EDialogButton
    {
        Ok
    }
}