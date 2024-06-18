using System;
using System.Collections.Generic;
using System.Globalization;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.SigGen;

namespace TapExtensions.Instruments.SigGen
{
    [Display("Windfreak SynthUSB3",
        Groups: new[] { "TapExtensions", "Instruments", "SigGen" },
        Description: "Windfreak SynthUSB3 RF Signal Generator, 12.5MHz to 6.4GHz")]
    public class WindfreakSynthUsb3 : WindfreakBase, ISigGen
    {
        // Frequency range
        private const double MinFreqMhz = 12.5;
        private const double MaxFreqMhz = 6400;
        private const double FreqResolutionHz = 0.1;

        // Amplitude range
        private const double MinAmplitude = -50;
        private const double MaxAmplitude = 8;
        private const double AmplitudeResolution = 0.25;

        private readonly object _internalInstLock = new object();
        private double _frequencyMhz;
        private bool _isOpen;

        public WindfreakSynthUsb3()
        {
            // Default values
            Name = "SynthUsb3";
            SerialPortName = "COM7";
            UseAutoDetection = true;
            UsbDeviceAddresses = new List<string> { @"USB\VID_16D0&PID_XXXX" };
            LoggingLevel = ELoggingLevel.Verbose;
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
            throw new NotImplementedException();
        }
    }
}