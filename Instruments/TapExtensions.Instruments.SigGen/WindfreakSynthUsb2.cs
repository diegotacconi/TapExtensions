using System;
using System.Globalization;
using System.IO.Ports;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.SigGen;

namespace TapExtensions.Instruments.SigGen
{
    [Display("Windfreak SynthUSB2",
        Groups: new[] { "TapExtensions", "Instruments", "SigGen" },
        Description: "Windfreak SynthUSB2 RF Signal Generator, 34MHz to 4.4GHz")]
    public class WindfreakSynthUsb2 : Instrument, ISigGen
    {
        #region Settings

        [Display("Serial Port Name", Order: 1)]
        public string SerialPortName { get; set; }

        [Display("Verbose Logging", Order: 2,
            Description: "Enables verbose logging of serial port communication.")]
        public bool VerboseLoggingEnabled { get; set; } = true;

        #endregion

        private const double MinFreqMhz = 35;
        private const double MaxFreqMhz = 4400;
        private readonly object _internalInstLock = new object();
        private SerialPort _sp;
        private double _frequencyMhz;

        public WindfreakSynthUsb2()
        {
            // Default values
            Name = "SynthUsb2";
            SerialPortName = "COM6";
        }

        public override void Open()
        {
            base.Open();
            OpenSerialPort();

            // +) Model Type
            Log.Debug("SgCw Model Type: " + SerialWriteRead("+").Trim('\n'));

            // -) Serial Number
            Log.Debug("SgCw Serial Number: " + SerialWriteRead("-").Trim('\n'));

            // v) show firmware version
            Log.Debug("SgCw Firmware Version: " + SerialWriteRead("v").Trim('\n'));

            // o) set RF On(1) or Off(0)
            SetRfOutputState(EState.Off);

            // h) set RF High(1) or Low(0) Power
            SerialWrite("h1");
            if (!SerialWriteRead("h?").Contains("1"))
                throw new Exception("Unable to set the SG RF Power to High");

            // a) set RF Power (0=mimimum, 3=maximum)
            SerialWrite("a3");
            if (!SerialWriteRead("a?").Contains("3"))
                throw new Exception("Unable to set the SG RF Power to maximum");

            // g) run sweep (on=1 / off=0)
            SerialWrite("g0");
            if (!SerialWriteRead("g?").Contains("0"))
                throw new Exception("Unable to set the SG sweep state to Off");

            // x) set internal reference (external=0 / internal=1)
            SerialWrite("x1");
            if (!SerialWriteRead("x?").Contains("1"))
                throw new Exception("Unable to set the SG internal reference to internal");

            // f) set RF Frequency
            SetFrequency(1000);
        }

        public override void Close()
        {
            if (_sp.IsOpen)
                SetRfOutputState(EState.Off);

            CloseSerialPort();
            base.Close();
        }

        private void OpenSerialPort()
        {
            // PNPDeviceID of "USB\VID_16D0&PID_0557\004571"
            // var comPort = GetComPort("16D0", "0557");
            /*
            string comPort;
            try
            {
                comPort = GetComPort("16D0", "0557");
            }
            catch (Exception ex)
            {
                Log.Warning($"--- {ex.Message} ---");
                comPort = GetComPort("16D0", "0557");
            }
            */
            var comPort = SerialPortName;

            _sp = new SerialPort
            {
                PortName = comPort,
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.RequestToSend,
                ReadTimeout = 2000,
                WriteTimeout = 2000,
                DtrEnable = true,
                RtsEnable = true
            };

            // Close serial port
            CloseSerialPort();

            // Open serial port
            if (VerboseLoggingEnabled)
                Log.Debug($"Opening serial port ({_sp.PortName}) with BaudRate={_sp.BaudRate}, Parity={_sp.Parity}, " +
                          $"DataBits={_sp.DataBits}, StopBits={_sp.StopBits}, Handshake={_sp.Handshake}");
            _sp.Open();
            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
        }

        private void CloseSerialPort()
        {
            if (_sp.IsOpen)
            {
                if (VerboseLoggingEnabled)
                    Log.Debug($"Closing serial port ({_sp.PortName})");

                // Close serial port
                _sp.DiscardInBuffer();
                _sp.DiscardOutBuffer();
                _sp.Close();
                _sp.Dispose();
            }
        }

        public double GetFrequency()
        {
            var freqReplyMhz = _frequencyMhz;

            lock (_internalInstLock)
            {
                var freqReply = SerialWriteRead("f?");
                if (double.TryParse(freqReply, out var freqReplyKhz))
                    freqReplyMhz = freqReplyKhz * 0.001;
                else
                    Log.Warning($"Unable to parse {freqReply}");
            }

            return freqReplyMhz;
        }

        public double GetOutputLevel()
        {
            throw new NotImplementedException();
        }

        public EState GetRfOutputState()
        {
            throw new NotImplementedException();
        }

        public void SetFrequency(double freqInMhz) //frequencyMhz)
        {
            // Check if frequency is is out-of-range
            if (freqInMhz < MinFreqMhz)
                throw new ArgumentOutOfRangeException(nameof(freqInMhz),
                    $@"Cannot set frequency below {MinFreqMhz} MHz");
            if (freqInMhz > MaxFreqMhz)
                throw new ArgumentOutOfRangeException(nameof(freqInMhz),
                    $@"Cannot set frequency above {MaxFreqMhz} MHz");

            lock (_internalInstLock)
            {
                // Set frequency
                //  Example: a communication for programming the frequency to 1GHz would be sent as "f1000.0"
                //   Please keep in mind that the device expects the format shown. For example if you send
                //   simply just an "f" the device will sit there and wait for the rest of the data and may
                //   appear locked up. If you dont send the decimal point and at least one digit afterward, it
                //   will have unexpected results. Also, please send data without hidden characters such as a
                //   carriage return at the end.
                SerialWrite("f" + freqInMhz.ToString("0.0########", CultureInfo.InvariantCulture));

                // A delay may be needed for the instrument to complete the previous command
                // TapThread.Sleep(100);

                // Check frequency
                var freqReplyMhz = GetFrequency();
                const double tolerance = 1e-6;
                if (Math.Abs(freqInMhz - freqReplyMhz) > tolerance)
                {
                    Log.Warning($"Unable to set the SG frequency to {freqInMhz} MHz");
                    _frequencyMhz = freqReplyMhz;
                }
                else
                {
                    _frequencyMhz = freqInMhz;
                }

                Log.Debug($"Set frequency to {_frequencyMhz} MHz");
            }
        }

        public void SetOutputLevel(double outputLevelDbm)
        {
            throw new NotImplementedException();
        }

        public void SetRfOutputState(EState outputState)// state)
        {
            lock (_internalInstLock)
            {
                if (outputState == EState.On)
                {
                    // Set output state
                    SerialWrite("o1");

                    // Set frequency
                    // Note: The output doesn't turn on if there is no freq command after the "o1" command
                    SetFrequency(_frequencyMhz);

                    // Check output state (On=1 / Off=0)
                    if (!SerialWriteRead("o?").Contains("1"))
                        throw new Exception("Unable to set the SG output state to On");

                    // Check phase lock status (lock=1 / unlock=0)
                    if (!SerialWriteRead("p").Contains("1"))
                        throw new Exception("Unable to set the SG output state to On (phase unlocked)");
                }
                else
                {
                    // Set output state
                    SerialWrite("o0");

                    // Check output state (On=1 / Off=0)
                    if (!SerialWriteRead("o?").Contains("0"))
                        throw new Exception("Unable to set the SG output state to Off");

                    // Check phase lock status (lock=1 / unlock=0)
                    if (!SerialWriteRead("p").Contains("0"))
                        throw new Exception("Unable to set the SG output state to Off (phase locked)");
                }

                Log.Debug($"Set RF output state to {outputState}");
            }

        }

        private string SerialWriteRead(string write)
        {
            SerialWrite(write);

            var received = string.Empty;
            const int timeoutMs = 3000;
            const int intervalMs = 10;
            const int maxCount = timeoutMs / intervalMs;
            var loopCount = 0;
            do
            {
                loopCount++;
                received += _sp.ReadExisting();
                TapThread.Sleep(intervalMs);
            } while (!received.Contains("\n") && loopCount < maxCount);

            if (VerboseLoggingEnabled)
                Log.Debug("{0} << {1}", _sp.PortName, received.Trim('\n'));

            return received;
        }

        private void SerialWrite(string write)
        {
            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
            _sp.WriteLine(write);

            if (VerboseLoggingEnabled)
                Log.Debug("{0} >> {1}", _sp.PortName, write);
        }
    }
}