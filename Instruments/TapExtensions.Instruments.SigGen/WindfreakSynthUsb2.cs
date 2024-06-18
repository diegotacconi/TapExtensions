using System;
using System.Collections.Generic;
using System.Globalization;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.SigGen;

namespace TapExtensions.Instruments.SigGen
{
    [Display("Windfreak SynthUSB2",
        Groups: new[] { "TapExtensions", "Instruments", "SigGen" },
        Description: "Windfreak SynthUSB2 RF Signal Generator, 34MHz to 4.4GHz")]
    public class WindfreakSynthUsb2 : WindfreakBase, ISigGen
    {
        // Frequency range
        private const double MinFreqMhz = 35;
        private const double MaxFreqMhz = 4400;

        // Default amplitude (in dBm), when power is set to 'a3', and frequency is set to 1 GHz
        private const double DefaultAmplitude = 1;

        private readonly object _internalInstLock = new object();
        private double _frequencyMhz;
        private bool _isOpen;

        public WindfreakSynthUsb2()
        {
            // Default values
            Name = "SynthUsb2";
            SerialPortName = "COM6";
            UseAutoDetection = true;
            UsbDeviceAddresses = new List<string> { @"USB\VID_16D0&PID_0557" };
            LoggingLevel = ELoggingLevel.Verbose;
        }

        public override void Open()
        {
            base.Open();

            if (LoggingLevel >= ELoggingLevel.Normal)
            {
                // +) Model Type
                Log.Debug("Model Type: " + SerialQuery("+").Trim('\n'));

                // -) Serial Number
                Log.Debug("Serial Number: " + SerialQuery("-").Trim('\n'));

                // v) show firmware version
                Log.Debug("Firmware Version: " + SerialQuery("v").Trim('\n'));
            }

            // o) set RF On(1) or Off(0)
            SetRfOutputState(EState.Off);

            // h) set RF High(1) or Low(0) Power
            SerialCommand("h1");
            if (!SerialQuery("h?").Contains("1"))
                throw new InvalidOperationException("Unable to set the SG RF Power to High");

            // a) set amplitude
            SetOutputLevel(DefaultAmplitude);

            // g) run sweep (on=1 / off=0)
            SerialCommand("g0");
            if (!SerialQuery("g?").Contains("0"))
                throw new InvalidOperationException("Unable to set the SG sweep state to Off");

            // x) set internal reference (external=0 / internal=1)
            SerialCommand("x1");
            if (!SerialQuery("x?").Contains("1"))
                throw new InvalidOperationException("Unable to set the SG internal reference to internal");

            // f) set frequency
            SetFrequency(1000);

            _isOpen = true;
        }

        public override void Close()
        {
            if (_isOpen)
                SetRfOutputState(EState.Off);

            base.Close();
        }

        public double GetFrequency()
        {
            double freqMhz;

            lock (_internalInstLock)
            {
                var response = SerialQuery("f?");
                if (!double.TryParse(response, out var freqKhz))
                    throw new InvalidOperationException($"Unable to parse response of '{response}'");

                freqMhz = freqKhz * 0.001;
            }

            return freqMhz;
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
            // Check if frequency is out-of-range
            if (frequencyMhz < MinFreqMhz)
                throw new InvalidOperationException($"Cannot set frequency below {MinFreqMhz} MHz.");
            if (frequencyMhz > MaxFreqMhz)
                throw new InvalidOperationException($"Cannot set frequency above {MaxFreqMhz} MHz.");

            lock (_internalInstLock)
            {
                // Set frequency
                //  Example: a communication for programming the frequency to 1GHz would be sent as "f1000.0"
                //   Please keep in mind that the device expects the format shown. For example if you send
                //   simply just an "f" the device will sit there and wait for the rest of the data and may
                //   appear locked up. If you don't send the decimal point and at least one digit afterward,
                //   it will have unexpected results. Also, please send data without hidden characters such
                //   as a carriage return at the end.
                SerialCommand("f" + frequencyMhz.ToString("0.0########", CultureInfo.InvariantCulture));

                // A delay may be needed for the instrument to complete the previous command
                // TapThread.Sleep(100);

                // Check frequency
                var freqReplyMhz = GetFrequency();
                _frequencyMhz = freqReplyMhz;

                if (LoggingLevel >= ELoggingLevel.Normal)
                {
                    const double tolerance = 1e-6;
                    if (Math.Abs(frequencyMhz - freqReplyMhz) > tolerance)
                        Log.Warning($"Set frequency to {freqReplyMhz} MHz, with a frequency error of " +
                                    $"{Math.Round(Math.Abs(frequencyMhz - freqReplyMhz) * 1e+6, 3)} Hz, " +
                                    $"for the requested frequency of {frequencyMhz} MHz");
                    else
                        Log.Debug($"Set frequency to {freqReplyMhz} MHz");
                }
            }
        }

        public void SetOutputLevel(double outputLevelDbm)
        {
            const double stepAmplitude = 3; // Step (in dB), between power levels of 'a3', 'a2', 'a1', and 'a0'.
            const double maxAmplitude = DefaultAmplitude + 0.5 * stepAmplitude;
            const double highAmplitude = DefaultAmplitude - 0.5 * stepAmplitude;
            const double midAmplitude = DefaultAmplitude - 1.5 * stepAmplitude;
            const double lowAmplitude = DefaultAmplitude - 2.5 * stepAmplitude;
            const double minAmplitude = DefaultAmplitude - 3.5 * stepAmplitude;

            // Check if amplitude is out-of-range
            if (outputLevelDbm > maxAmplitude)
                throw new InvalidOperationException($"Cannot set amplitude above {maxAmplitude} dBm");
            if (outputLevelDbm < minAmplitude)
                throw new InvalidOperationException($"Cannot set amplitude below {minAmplitude} dBm");

            var a = 3;
            var coarseAmplitude = DefaultAmplitude;
            switch (outputLevelDbm)
            {
                case double x when x >= highAmplitude:
                    coarseAmplitude = DefaultAmplitude;
                    a = 3;
                    break;

                case double x when x < highAmplitude && x >= midAmplitude:
                    coarseAmplitude = DefaultAmplitude - 1 * stepAmplitude;
                    a = 2;
                    break;

                case double x when x < midAmplitude && x >= lowAmplitude:
                    coarseAmplitude = DefaultAmplitude - 2 * stepAmplitude;
                    a = 1;
                    break;

                case double x when x < lowAmplitude:
                    coarseAmplitude = DefaultAmplitude - 3 * stepAmplitude;
                    a = 0;
                    break;
            }

            lock (_internalInstLock)
            {
                // Set amplitude
                SerialCommand($"a{a:D1}");

                // Check amplitude
                if (!SerialQuery("a?").Contains($"{a:D1}"))
                    throw new InvalidOperationException($"Unable to set amplitude to 'a{a:D1}'");

                if (LoggingLevel >= ELoggingLevel.Normal)
                {
                    const double tolerance = 0.01;
                    if (Math.Abs(outputLevelDbm - coarseAmplitude) > tolerance)
                        Log.Warning($"Set amplitude to approximately {coarseAmplitude} dBm, " +
                                    $"when range is within ({coarseAmplitude - 0.5 * stepAmplitude}, " +
                                    $"{coarseAmplitude + 0.5 * stepAmplitude}), " +
                                    $"for the requested amplitude of {outputLevelDbm} dBm");
                    else
                        Log.Debug($"Set amplitude to approximately {coarseAmplitude} dBm");
                }
            }
        }

        public void SetRfOutputState(EState state)
        {
            lock (_internalInstLock)
            {
                if (state == EState.On)
                {
                    // Set output state
                    SerialCommand("o1");

                    // Set frequency
                    // Note: The output doesn't turn on if there is no freq command after the "o1" command
                    SetFrequency(_frequencyMhz);

                    // Check output state (On=1 / Off=0)
                    if (!SerialQuery("o?").Contains("1"))
                        throw new InvalidOperationException("Unable to set the RF output state to On");

                    // Check phase lock status (lock=1 / unlock=0)
                    if (!SerialQuery("p").Contains("1"))
                        throw new InvalidOperationException("Unable to set the RF output state to On (phase unlocked)");
                }
                else
                {
                    // Set output state
                    SerialCommand("o0");

                    // Check output state (On=1 / Off=0)
                    if (!SerialQuery("o?").Contains("0"))
                        throw new InvalidOperationException("Unable to set the RF output state to Off");

                    // Check phase lock status (lock=1 / unlock=0)
                    if (!SerialQuery("p").Contains("0"))
                        throw new InvalidOperationException("Unable to set the RF output state to Off (phase locked)");
                }

                if (LoggingLevel >= ELoggingLevel.Normal)
                    Log.Debug($"Set RF output state to {state}");
            }
        }
    }
}