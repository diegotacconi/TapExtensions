using System;
using System.Collections.Generic;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.SigGen;

namespace TapExtensions.Instruments.SigGen
{
    [Display("UsbSerialDev5",
        Groups: new[] { "TapExtensions", "Instruments", "SigGen" })]
    public class UsbSerialDev5 : WindfreakBase, ISigGen
    {
        public UsbSerialDev5()
        {
            // Default values
            Name = nameof(UsbSerialDev5);
            SerialPortName = "COM4";
            UsbDeviceAddresses = new List<string>
            {
                @"USB\VID_1EAB&PID_1D06\CF078472"
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