﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.DcPwr;

namespace TapExtensions.Instruments.DcPwr
{
    public abstract class KeysightDcPwr : ScpiInstrument, IDcPwr
    {
        protected double MaxCurrentA;
        protected double MaxVoltageV;
        protected double MinCurrentA;
        protected double MinVoltageV;

        protected void Open(string expectedIdn, string resourceName)
        {
            base.Open();

            if (!IdnString.Contains(expectedIdn))
            {
                var msg = $"The {resourceName} instrument driver does not support the connected instrument " +
                          $"with IDN of {IdnString}.";
                Log.Error(msg);
                throw new InvalidOperationException(msg);
            }
        }

        protected void GetMaxMinValues()
        {
            MaxVoltageV = ScpiQuery<double>("VOLT? MAX");
            MaxCurrentA = ScpiQuery<double>("CURR? MAX");
            MinVoltageV = ScpiQuery<double>("VOLT? MIN");
            MinCurrentA = ScpiQuery<double>("CURR? MIN");
        }

        protected void SetAndClearProtections()
        {
            // Set over-current protection
            if (GetCurrentProtectionState() != EState.On)
                SetCurrentProtectionState(EState.On);

            // Set over-voltage protection
            if (GetVoltageProtectionState() != EState.On)
                SetVoltageProtectionState(EState.On);

            // Clear over-current protection if tripped
            if (GetCurrentProtectionTripped() == EState.On)
            {
                Log.Warning("The over-current protection circuit has tripped");

                if (GetOutputState() == EState.On)
                    SetOutputState(EState.Off);

                ClearCurrentProtection();

                if (GetCurrentProtectionTripped() == EState.On)
                    throw new InvalidOperationException(
                        "Unable to clear the over-current protection");
            }

            // Clear over-voltage protection if tripped
            if (GetVoltageProtectionTripped() == EState.On)
            {
                Log.Warning("The over-voltage protection circuit has tripped");

                if (GetOutputState() == EState.On)
                    SetOutputState(EState.Off);

                ClearVoltageProtection();

                if (GetVoltageProtectionTripped() == EState.On)
                    throw new InvalidOperationException(
                        "Unable to clear the over-voltage protection");
            }
        }

        protected void SetVoltageRange(string voltageRange)
        {
            ScpiCommand($"VOLT:RANG {voltageRange}");
        }

        public override void Close()
        {
            // Check for errors
            DcPwrQueryErrors();

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

        private void DcPwrQueryErrors(int maxErrors = 1000)
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

        #region Over-Current and Over-Voltage Protections

        private EState GetCurrentProtectionState()
        {
            var response = ScpiQuery<short>("CURR:PROT:STAT?");
            if (response == 0)
                return EState.Off;

            if (response == 1)
                return EState.On;

            throw new InvalidOperationException(
                $"{nameof(GetCurrentProtectionState)} was not able to determine the response from '{response}'");
        }

        private EState GetVoltageProtectionState()
        {
            var response = ScpiQuery<short>("VOLT:PROT:STAT?");
            if (response == 0)
                return EState.Off;

            if (response == 1)
                return EState.On;

            throw new InvalidOperationException(
                $"{nameof(GetVoltageProtectionState)} was not able to determine the response from '{response}'");
        }

        private void SetCurrentProtectionState(EState state)
        {
            ScpiCommand(EState.On == state ? "CURR:PROT:STAT ON" : "CURR:PROT:STAT OFF");
        }

        private void SetVoltageProtectionState(EState state)
        {
            ScpiCommand(EState.On == state ? "VOLT:PROT:STAT ON" : "VOLT:PROT:STAT OFF");
        }

        private EState GetCurrentProtectionTripped()
        {
            var response = ScpiQuery<short>("CURR:PROT:TRIP?");
            if (response == 0)
                return EState.Off;

            if (response == 1)
                return EState.On;

            throw new InvalidOperationException(
                $"{nameof(GetCurrentProtectionTripped)} was not able to determine the response from '{response}'");
        }

        private EState GetVoltageProtectionTripped()
        {
            var response = ScpiQuery<short>("VOLT:PROT:TRIP?");
            if (response == 0)
                return EState.Off;

            if (response == 1)
                return EState.On;

            throw new InvalidOperationException(
                $"{nameof(GetVoltageProtectionTripped)} was not able to determine the response from '{response}'");
        }

        private void ClearCurrentProtection()
        {
            ScpiCommand("CURR:PROT:CLE");
        }

        private void ClearVoltageProtection()
        {
            ScpiCommand("VOLT:PROT:CLE");
        }

        #endregion
    }
}