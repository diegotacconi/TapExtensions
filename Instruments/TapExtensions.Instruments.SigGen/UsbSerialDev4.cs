using System;
using System.Collections.Generic;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.SigGen;

namespace TapExtensions.Instruments.SigGen
{
    [Display("UsbSerialDev4",
        Groups: new[] { "TapExtensions", "Instruments", "SigGen" })]
    public class UsbSerialDev4 : WindfreakBase, ISigGen
    {
        public UsbSerialDev4()
        {
            // Default values
            Name = nameof(UsbSerialDev4);
            SerialPortName = "COM3";
            UsbDeviceAddresses = new List<string>
            {
                @"USB\VID_05E0&PID_1701\USB_CDC_SYMBOL_SCANNER"
            };
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
            throw new NotImplementedException();
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