using System;
using System.Collections.Generic;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.SigGen;

namespace TapExtensions.Instruments.SigGen
{
    [Display("UsbSerialDev1",
        Groups: new[] { "TapExtensions", "Instruments", "SigGen" })]
    public class UsbSerialDev1 : WindfreakBase, ISigGen
    {
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