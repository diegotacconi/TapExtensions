using System;
using System.Collections.Generic;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.SigGen;

namespace TapExtensions.Instruments.SigGen
{
    [Display("UsbSerialDev2",
        Groups: new[] { "TapExtensions", "Instruments", "SigGen" })]
    public class UsbSerialDev2 : WindfreakBase, ISigGen
    {
        public UsbSerialDev2()
        {
            // Default values
            Name = nameof(UsbSerialDev2);
            SerialPortName = "COM7";
            UsbDeviceAddresses = new List<string>
            {
                @"USB\VID_10C4&PID_EA70&MI_00\A&6C616C5&0&0000"

                /*
                  USB\VID_10C4&PID_EA70&MI_00\A&6C616C5&0&0000
                  USB\VID_10C4&PID_EA70&MI_01\A&6C616C5&0&0001

                  USB\VID_10C4&PID_EA70&MI_00\6&1DB4B887&0&0000
                  USB\VID_10C4&PID_EA70&MI_01\6&1DB4B887&0&0001
                 */
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