using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.SigGen;
using TapExtensions.Shared.Win32;

namespace TapExtensions.Instruments.SigGen
{
    [Display("Windfreak SynthUSB3",
        Groups: new[] { "TapExtensions", "Instruments", "SigGen" },
        Description: "Windfreak SynthUSB3 RF Signal Generator, 12MHz to 6.4GHz")]
    public class WindfreakSynthUsb3 : Instrument, ISigGen
    {
        #region Settings

        [Display("Serial Port Auto-Detection", Order: 1)]
        public bool AutoDetection { get; set; } = false;

        [EnabledIf(nameof(AutoDetection), false, HideIfDisabled = true)]
        [Display("Serial Port Name", Order: 2)]
        public string SerialPortName { get; set; }

        [EnabledIf(nameof(AutoDetection), true, HideIfDisabled = true)]
        [Display("Search for USB Devices", Order: 3)]
        public List<string> SearchForUsbInstancePaths { get; set; }

        [EnabledIf(nameof(AutoDetection), true, HideIfDisabled = true)]
        [Display("Show available USB Devices", Order: 4, Group: "Try Auto-Detection", Collapsed: true)]
        [Browsable(true)]
        public void ShowAvailableUsbDevicesButton()
        {
            ShowAvailableUsbDevices();
        }

        [EnabledIf(nameof(AutoDetection), true, HideIfDisabled = true)]
        [Display("Search Now", Order: 5, Group: "Try Auto-Detection", Collapsed: true)]
        [Browsable(true)]
        public void SearchNowButton()
        {
            SearchNow();
        }

        public enum ELoggingLevel : int
        {
            None = 0,
            Normal = 1,
            Verbose = 2
        }

        [Display("Logging Level", Order: 6, Group: "Debug", Collapsed: true,
            Description: "Level of verbose logging for serial port (UART) communication.")]
        public ELoggingLevel LoggingLevel { get; set; }

        #endregion

        public WindfreakSynthUsb3()
        {
            // Default values
            Name = "SynthUsb3";
            SerialPortName = "COM6";

            SearchForUsbInstancePaths = new List<string>
            {
                @"USB\VID_16D0&PID_0557\004571",
                @"USB\VID_16D0&PID_0557"
            };

            LoggingLevel = ELoggingLevel.Verbose;
        }

        public override void Open()
        {
            base.Open();

            // ComPort = "COM16", InstancePath = @"USB\VID_16D0&PID_0557\004571", Description = "USB Serial Device"
            SearchNow();

        }

        private void ShowAvailableUsbDevices()
        {
            var devices = UsbDevices.GetAllSerialDevices();
            foreach (var device in devices)
                Log.Debug($"'{device.ComPort}', '{device.InstancePath}', '{device.Description}'");
        }

        private void SearchNow()
        {

            if (LoggingLevel >= ELoggingLevel.Verbose)
                Log.Debug("Searching for USB Instance Path(s) of " +
                          $"'{string.Join("', '", SearchForUsbInstancePaths)}'");

            var found = UsbDevices.FindInstancePath(SearchForUsbInstancePaths);

            if (LoggingLevel >= ELoggingLevel.Normal)
                Log.Debug($"Found serial port at '{found.ComPort}' " +
                          $"with USB Instance Path of '{found.InstancePath}' " +
                          $"and Description of '{found.Description}'");

            SerialPortName = found.ComPort;
            SerialPortName = found.ComPort;
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