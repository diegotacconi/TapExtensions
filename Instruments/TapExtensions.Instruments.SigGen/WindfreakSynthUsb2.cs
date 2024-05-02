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
    public class WindfreakSynthUsb2 : SerialInstrument, ISigGen
    {
        private const double MinFreqMhz = 35;
        private const double MaxFreqMhz = 4400;
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
            LoggingLevel = ELoggingLevel.Normal;
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

            // a) set RF Power (0=minimum, 3=maximum)
            SerialCommand("a3");
            if (!SerialQuery("a?").Contains("3"))
                throw new InvalidOperationException("Unable to set the SG RF Power to maximum");

            // g) run sweep (on=1 / off=0)
            SerialCommand("g0");
            if (!SerialQuery("g?").Contains("0"))
                throw new InvalidOperationException("Unable to set the SG sweep state to Off");

            // x) set internal reference (external=0 / internal=1)
            SerialCommand("x1");
            if (!SerialQuery("x?").Contains("1"))
                throw new InvalidOperationException("Unable to set the SG internal reference to internal");

            // f) set RF Frequency
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
            var freqReplyMhz = _frequencyMhz;

            lock (_internalInstLock)
            {
                var freqReply = SerialQuery("f?");
                if (double.TryParse(freqReply, out var freqReplyKhz))
                    freqReplyMhz = freqReplyKhz * 0.001;
                else
                    Log.Warning($"Unable to parse {freqReply}");
            }

            return freqReplyMhz;
        }

        public double GetOutputLevel()
        {
            throw new NotSupportedException();
        }

        public EState GetRfOutputState()
        {
            throw new NotImplementedException();
        }

        public void SetFrequency(double frequencyMhz)
        {
            // Check if frequency is is out-of-range
            if (frequencyMhz < MinFreqMhz)
                throw new ArgumentOutOfRangeException(nameof(frequencyMhz),
                    $@"Cannot set frequency below {MinFreqMhz} MHz");
            if (frequencyMhz > MaxFreqMhz)
                throw new ArgumentOutOfRangeException(nameof(frequencyMhz),
                    $@"Cannot set frequency above {MaxFreqMhz} MHz");

            lock (_internalInstLock)
            {
                // Set frequency
                //  Example: a communication for programming the frequency to 1GHz would be sent as "f1000.0"
                //   Please keep in mind that the device expects the format shown. For example if you send
                //   simply just an "f" the device will sit there and wait for the rest of the data and may
                //   appear locked up. If you don't send the decimal point and at least one digit afterward, it
                //   will have unexpected results. Also, please send data without hidden characters such as a
                //   carriage return at the end.
                SerialCommand("f" + frequencyMhz.ToString("0.0########", CultureInfo.InvariantCulture));

                // A delay may be needed for the instrument to complete the previous command
                // TapThread.Sleep(100);

                // Check frequency
                var freqReplyMhz = GetFrequency();
                const double tolerance = 1e-6;
                if (Math.Abs(frequencyMhz - freqReplyMhz) > tolerance)
                {
                    Log.Warning($"Unable to set the SG frequency to {frequencyMhz} MHz");
                    _frequencyMhz = freqReplyMhz;
                }
                else
                {
                    _frequencyMhz = frequencyMhz;
                }

                if (LoggingLevel >= ELoggingLevel.Normal)
                    Log.Debug($"Set frequency to {_frequencyMhz} MHz");
            }
        }

        public void SetOutputLevel(double outputLevelDbm)
        {
            throw new NotSupportedException();
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
                        throw new InvalidOperationException("Unable to set the SG output state to On");

                    // Check phase lock status (lock=1 / unlock=0)
                    if (!SerialQuery("p").Contains("1"))
                        throw new InvalidOperationException("Unable to set the SG output state to On (phase unlocked)");
                }
                else
                {
                    // Set output state
                    SerialCommand("o0");

                    // Check output state (On=1 / Off=0)
                    if (!SerialQuery("o?").Contains("0"))
                        throw new InvalidOperationException("Unable to set the SG output state to Off");

                    // Check phase lock status (lock=1 / unlock=0)
                    if (!SerialQuery("p").Contains("0"))
                        throw new InvalidOperationException("Unable to set the SG output state to Off (phase locked)");
                }

                if (LoggingLevel >= ELoggingLevel.Normal)
                    Log.Debug($"Set RF output state to {state}");
            }
        }
    }
}