using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.DcPwr;

namespace TapExtensions.Instruments.DcPwr
{
    public abstract class KeysightE3630Base : ScpiInstrument, IDcPwr
    {
        protected double MaxCurrentA;
        protected double MaxVoltageV;
        protected double MinCurrentA;
        protected double MinVoltageV;

        protected KeysightE3630Base()
        {
            // Default values
            VerboseLoggingEnabled = true;
        }

        protected void Open(string expectedIdn, string resourceName, string voltageRangeChoice)
        {
            base.Open();

            if (!IdnString.Contains(expectedIdn))
            {
                var msg = $"The {resourceName} instrument driver does not support the connected instrument " +
                          $"with IDN of {IdnString}.";
                Log.Error(msg);
                throw new InvalidOperationException(msg);
            }

            // Check voltage range
            var voltageRange = ScpiQuery<string>("VOLTage:RANGe?");

            if (voltageRange != voltageRangeChoice)
            {
                // Select voltage range
                ScpiCommand($"VOLTage:RANGe {voltageRangeChoice}");

                // Verify voltage range selection
                voltageRange = ScpiQuery<string>("VOLTage:RANGe?");
                if (voltageRange != voltageRangeChoice)
                    throw new InvalidOperationException("Unable to select desired voltage range.");
            }

            // Query max and min values
            MaxVoltageV = ScpiQuery<double>("VOLT? MAX");
            MaxCurrentA = ScpiQuery<double>("CURR? MAX");
            MinVoltageV = ScpiQuery<double>("VOLT? MIN");
            MinCurrentA = ScpiQuery<double>("CURR? MIN");
        }

        public override void Close()
        {
            // Check for errors
            PsuQueryErrors();

            base.Close();
        }

        public EState GetOutputState()
        {
            var response = ScpiQuery<short>("OUTP:STAT?");
            if (response == 0)
                return EState.Off;

            if (response == 1)
                return EState.On;

            throw new InvalidOperationException(
                $"{nameof(GetOutputState)} was not able to determine the response from '{response}'");
        }

        public double MeasureCurrent()
        {
            return ScpiQuery<double>("MEAS:CURR?");
        }

        public double MeasureVoltage()
        {
            return ScpiQuery<double>("MEAS:VOLT?");
        }

        public void SetCurrent(double currentAmps)
        {
            if (currentAmps < MinCurrentA || currentAmps > MaxCurrentA)
                throw new ArgumentOutOfRangeException(nameof(currentAmps),
                    $@"The current value of {currentAmps} is not in the valid range of {MinCurrentA} to {MaxCurrentA}");

            ScpiCommand(Scpi.Format("CURR {0}", currentAmps));
        }

        public void SetVoltage(double voltageVolts)
        {
            if (voltageVolts < MinVoltageV || voltageVolts > MaxVoltageV)
                throw new ArgumentOutOfRangeException(nameof(voltageVolts),
                    $@"The voltage value of {voltageVolts} is not in the valid range of {MinVoltageV} to {MaxVoltageV}");

            ScpiCommand(Scpi.Format("VOLT {0}", voltageVolts));
        }

        public void SetOutputState(EState state)
        {
            ScpiCommand(EState.On == state ? "OUTP:STAT ON" : "OUTP:STAT OFF");
        }

        private void PsuQueryErrors(int maxErrors = 1000)
        {
            IList<ScpiError> errors = Array.Empty<ScpiError>();
            while (errors.Count < maxErrors)
            {
                var error = QueryError();

                if (error.Code == 0)
                    break;

                Log.Error($"Error = {error}");
            }
        }

        private ScpiError QueryError()
        {
            int errorCode;
            string errorMsg;
            var errorStr = ScpiQuery("SYST:ERR?").Trim();
            var regexMatch = Regex.Match(errorStr, "(?<code>[\\-\\+0-9]+),\"(?<msg>.+)\"");
            if (regexMatch.Success)
            {
                errorMsg = regexMatch.Groups["msg"].Value;
                var success = int.TryParse(regexMatch.Groups["code"].Value, out errorCode);
                if (!success)
                    errorCode = 0;
            }
            else
            {
                errorMsg = errorStr;
                errorCode = 0;
            }

            return new ScpiError { Code = errorCode, Message = errorMsg };
        }
    }
}