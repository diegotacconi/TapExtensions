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

        public enum ELoggingLevel : int
        {
            None = 0,
            Normal = 1,
            Verbose = 2
        }

        [Display("Logging Level", Order: 20, Group: "Debug", Collapsed: true,
            Description: "Level of verbose logging for serial port (UART) communication.")]
        public ELoggingLevel LoggingLevel { get; set; }

        #endregion

        private SerialPort _sp;
        private const double MinFreqMhz = 35;
        private const double MaxFreqMhz = 4400;
        private readonly object _internalInstLock = new object();
        private double _frequencyMhz;

        public WindfreakSynthUsb2()
        {
            // Default values
            Name = "SynthUsb2";
            SerialPortName = "COM6";
            LoggingLevel = ELoggingLevel.Verbose;
        }

        public override void Open()
        {
            base.Open();
            OpenSerialPort();

            if (LoggingLevel >= ELoggingLevel.Normal)
            {
                // +) Model Type
                Log.Debug("Model Type: " + WriteRead("+").Trim('\n'));

                // -) Serial Number
                Log.Debug("Serial Number: " + WriteRead("-").Trim('\n'));

                // v) show firmware version
                Log.Debug("Firmware Version: " + WriteRead("v").Trim('\n'));
            }

            // o) set RF On(1) or Off(0)
            SetRfOutputState(EState.Off);

            // h) set RF High(1) or Low(0) Power
            Write("h1");
            if (!WriteRead("h?").Contains("1"))
                throw new InvalidOperationException("Unable to set the SG RF Power to High");

            // a) set RF Power (0=minimum, 3=maximum)
            Write("a3");
            if (!WriteRead("a?").Contains("3"))
                throw new InvalidOperationException("Unable to set the SG RF Power to maximum");

            // g) run sweep (on=1 / off=0)
            Write("g0");
            if (!WriteRead("g?").Contains("0"))
                throw new InvalidOperationException("Unable to set the SG sweep state to Off");

            // x) set internal reference (external=0 / internal=1)
            Write("x1");
            if (!WriteRead("x?").Contains("1"))
                throw new InvalidOperationException("Unable to set the SG internal reference to internal");

            // f) set RF Frequency
            SetFrequency(1000);
        }

        private void OpenSerialPort()
        {
            _sp = new SerialPort
            {
                PortName = SerialPortName,
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.RequestToSend,
                ReadTimeout = 2000, // 2 second
                WriteTimeout = 2000, // 2 second
                DtrEnable = true,
                RtsEnable = true
            };

            // Close serial port if already opened
            CloseSerialPort();

            switch (LoggingLevel)
            {
                case ELoggingLevel.Normal:
                    Log.Debug($"Opening serial port ({_sp.PortName})");
                    break;

                case ELoggingLevel.Verbose:
                    Log.Debug($"Opening serial port ({_sp.PortName}) with BaudRate={_sp.BaudRate}, " +
                              $"Parity={_sp.Parity}, DataBits={_sp.DataBits}, StopBits={_sp.StopBits}, " +
                              $"Handshake={_sp.Handshake}");
                    break;
            }

            // Open serial port
            _sp.Open();
            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
        }

        public override void Close()
        {
            if (_sp.IsOpen)
                SetRfOutputState(EState.Off);

            CloseSerialPort();
            base.Close();
        }

        private void CloseSerialPort()
        {
            try
            {
                if (_sp.IsOpen)
                {
                    if (LoggingLevel >= ELoggingLevel.Normal)
                        Log.Debug($"Closing serial port ({_sp.PortName})");

                    // Close serial port
                    _sp.DiscardInBuffer();
                    _sp.DiscardOutBuffer();
                    _sp.Close();
                    _sp.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex.Message);
            }
        }

        public double GetFrequency()
        {
            var freqReplyMhz = _frequencyMhz;

            lock (_internalInstLock)
            {
                var freqReply = WriteRead("f?");
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
                Write("f" + frequencyMhz.ToString("0.0########", CultureInfo.InvariantCulture));

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
                    Write("o1");

                    // Set frequency
                    // Note: The output doesn't turn on if there is no freq command after the "o1" command
                    SetFrequency(_frequencyMhz);

                    // Check output state (On=1 / Off=0)
                    if (!WriteRead("o?").Contains("1"))
                        throw new InvalidOperationException("Unable to set the SG output state to On");

                    // Check phase lock status (lock=1 / unlock=0)
                    if (!WriteRead("p").Contains("1"))
                        throw new InvalidOperationException("Unable to set the SG output state to On (phase unlocked)");
                }
                else
                {
                    // Set output state
                    Write("o0");

                    // Check output state (On=1 / Off=0)
                    if (!WriteRead("o?").Contains("0"))
                        throw new InvalidOperationException("Unable to set the SG output state to Off");

                    // Check phase lock status (lock=1 / unlock=0)
                    if (!WriteRead("p").Contains("0"))
                        throw new InvalidOperationException("Unable to set the SG output state to Off (phase locked)");
                }

                if (LoggingLevel >= ELoggingLevel.Normal)
                    Log.Debug($"Set RF output state to {state}");
            }
        }

        private string WriteRead(string command)
        {
            Write(command);

            var response = string.Empty;
            const int timeoutMs = 3000;
            const int intervalMs = 10;
            const int maxCount = timeoutMs / intervalMs;
            var loopCount = 0;
            do
            {
                loopCount++;
                response += _sp.ReadExisting();
                TapThread.Sleep(intervalMs);
            } while (!response.Contains("\n") && loopCount < maxCount);

            if (LoggingLevel >= ELoggingLevel.Verbose)
                Log.Debug("{0} << {1}", _sp.PortName, response.Trim('\n'));

            return response;
        }

        private void Write(string command)
        {
            if (LoggingLevel >= ELoggingLevel.Verbose)
                Log.Debug("{0} >> {1}", _sp.PortName, command);

            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
            _sp.WriteLine(command);
        }
    }
}