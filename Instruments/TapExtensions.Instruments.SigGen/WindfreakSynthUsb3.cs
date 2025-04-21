// Windfreak SynthUSB3 RF Signal Generator
// https://windfreaktech.com/product/synthusb3-6ghz-rf-signal-generator/
//
// Serial Communication Specification
// https://windfreaktech.com/wp-content/uploads/2020/11/WFT_SynthUSB3_API_10a.pdf

using System;
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
        private const double DefaultFreqMhz = 1000;

        // Amplitude range
        private const double MinAmplitude = -50;
        private const double MaxAmplitude = 10; // Amplitude varies from +8 to +10 dBm, depending on frequency
        private const double AmplitudeResolution = 0.01; // Accuracy is 0.25 dB
        private const double DefaultAmplitude = 0;

        private static readonly object InstLock = new object();
        private bool _isOpen;

        public WindfreakSynthUsb3()
        {
            // Default values
            Name = "SynthUsb3";
            ConnectionAddress = @"USB\VID_0483&PID_A3E8";
            // Validation rules
            Rules.Add(ValidateConnectionAddress, "Not valid", nameof(ConnectionAddress));
        }

        public override void Open()
        {
            base.Open();

            lock (InstLock)
            {
                // +) Show model type
                Log.Debug("Model Type: " + SerialQuery("+").Trim('\n'));

                // -) Show serial number
                Log.Debug("Serial Number: " + SerialQuery("-").Trim('\n'));

                // v0) Show firmware version
                Log.Debug("Firmware Version: " + SerialQuery("v0").Trim('\n'));

                // v1) Shows hardware version
                Log.Debug("Hardware Version: " + SerialQuery("v1").Trim('\n'));

                // x) Set reference (0=external / 1=internal)
                SerialWrite("x1");
                if (!SerialQuery("x?").Contains("1"))
                    throw new InvalidOperationException("Unable to set reference to internal");

                SetRfOutputState(EState.Off);
                SetOutputLevel(DefaultAmplitude);
                SetFrequency(DefaultFreqMhz);
            }

            _isOpen = true;
        }

        public override void Close()
        {
            if (_isOpen)
                SetRfOutputState(EState.Off);

            _isOpen = false;
            base.Close();
        }

        public double GetFrequency()
        {
            double freqMhz;

            lock (InstLock)
            {
                var response = SerialQuery("f?");
                if (!double.TryParse(response, out freqMhz))
                    throw new InvalidOperationException($"Unable to parse response of '{response}'");
            }

            return freqMhz;
        }

        public double GetOutputLevel()
        {
            double amplitude;

            lock (InstLock)
            {
                var response = SerialQuery("W?");
                if (!double.TryParse(response, out amplitude))
                    throw new InvalidOperationException($"Unable to parse response of '{response}'");
            }

            return amplitude;
        }

        public EState GetRfOutputState()
        {
            lock (InstLock)
            {
                // Get output state (On=1 / Off=0)
                var response = SerialQuery("E?");

                switch (response)
                {
                    case string a when a.Contains("1"):
                        return EState.On;

                    case string b when b.Contains("0"):
                        return EState.Off;

                    default:
                        throw new InvalidOperationException("Unable to get the RF output state");
                }
            }
        }

        public void SetFrequency(double frequencyMhz)
        {
            // Check if frequency is out-of-range
            if (frequencyMhz < MinFreqMhz)
                throw new InvalidOperationException($"Cannot set frequency below {MinFreqMhz} MHz.");
            if (frequencyMhz > MaxFreqMhz)
                throw new InvalidOperationException($"Cannot set frequency above {MaxFreqMhz} MHz.");

            lock (InstLock)
            {
                // Set frequency
                SerialWrite("f" + frequencyMhz.ToString("0.0#######", CultureInfo.InvariantCulture));

                // Check frequency
                var freqReplyMhz = GetFrequency();

                // Check self-calibration
                if (!SerialQuery("V").Contains("1"))
                    throw new InvalidOperationException("Self-calibration failed (output not leveled)");

                const double tolerance = 1e-7;
                if (Math.Abs(frequencyMhz - freqReplyMhz) > tolerance)
                    Log.Warning($"Set frequency to {freqReplyMhz} MHz, with a frequency error of " +
                                $"{Math.Round(Math.Abs(frequencyMhz - freqReplyMhz) * 1e+6, 3)} Hz, " +
                                $"for the requested frequency of {frequencyMhz} MHz");
                else
                    Log.Debug($"Set frequency to {freqReplyMhz} MHz");
            }
        }

        public void SetOutputLevel(double outputLevelDbm)
        {
            // Check if amplitude is out-of-range
            if (outputLevelDbm > MaxAmplitude)
                throw new InvalidOperationException($"Cannot set amplitude above {MaxAmplitude} dBm");
            if (outputLevelDbm < MinAmplitude)
                throw new InvalidOperationException($"Cannot set amplitude below {MinAmplitude} dBm");

            lock (InstLock)
            {
                // Set amplitude
                SerialWrite("W" + outputLevelDbm.ToString("0.0##", CultureInfo.InvariantCulture));

                // Check amplitude
                var replyDbm = GetOutputLevel();

                // Check self-calibration
                if (!SerialQuery("V").Contains("1"))
                    throw new InvalidOperationException("Self-calibration failed (output not leveled)");

                if (Math.Abs(outputLevelDbm - replyDbm) > AmplitudeResolution)
                    Log.Warning($"Set amplitude to {replyDbm} dBm, with a amplitude error of " +
                                $"{Math.Round(Math.Abs(outputLevelDbm - replyDbm), 3)} dB, " +
                                $"for the requested amplitude of {outputLevelDbm} dBm");
                else
                    Log.Debug($"Set amplitude to {replyDbm} dBm");
            }
        }

        public void SetRfOutputState(EState state)
        {
            lock (InstLock)
            {
                if (state == EState.On)
                {
                    // Set output state
                    SerialWrite("E1");

                    // Check output state
                    if (GetRfOutputState() != EState.On)
                        throw new InvalidOperationException("Unable to set the RF output state to On");

                    // Check phase lock status (lock=1 / unlock=0)
                    if (!SerialQuery("p").Contains("1"))
                        throw new InvalidOperationException("Unable to set the RF output state to On (phase unlocked)");
                }
                else
                {
                    // Set output state
                    SerialWrite("E0");

                    // Check output state
                    if (GetRfOutputState() != EState.Off)
                        throw new InvalidOperationException("Unable to set the RF output state to Off");

                    // Check phase lock status (lock=1 / unlock=0)
                    if (!SerialQuery("p").Contains("0"))
                        throw new InvalidOperationException("Unable to set the RF output state to Off (phase locked)");
                }

                Log.Debug($"Set RF output state to {state}");
            }
        }
    }
}