using System;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.DcPwr;

namespace TapExtensions.Steps.DcPwr
{
    [Display("DcPowerOn",
        Groups: new[] { "TapExtensions", "Steps", "DcPwr" })]
    public class DcPowerOn : TestStep
    {
        #region Settings

        [Display("DcPwr", Order: 1, Group: "Instruments",
            Description: "DC Power Supply instrument interface")]
        public IDcPwr DcPwr { get; set; }

        [Display("Set Voltage", Order: 2, Group: "Parameters")]
        [Unit("V")]
        public double Voltage { get; set; }

        [Display("Set Current", Order: 3, Group: "Parameters")]
        [Unit("A")]
        public double Current { get; set; }

        [Display("Time Delay Before PowerOn", Order: 4, Group: "Parameters")]
        [Unit("s")]
        public double TimeDelayBeforePowerOn { get; set; }

        [Display("Time Delay After PowerOn", Order: 5, Group: "Parameters")]
        [Unit("s")]
        public double TimeDelayAfterPowerOn { get; set; }

        [Display("Voltage Low Limit", Order: 6, Group: "Limits")]
        [Unit("V")]
        public double VoltageLimitLow { get; set; }

        [Display("Voltage High Limit", Order: 7, Group: "Limits")]
        [Unit("V")]
        public double VoltageLimitHigh { get; set; }

        [Display("Current Low Limit", Order: 8, Group: "Limits")]
        [Unit("A")]
        public double CurrentLimitLow { get; set; }

        [Display("Current High Limit", Order: 9, Group: "Limits")]
        [Unit("A")]
        public double CurrentLimitHigh { get; set; }

        #endregion

        public DcPowerOn()
        {
            // Default values
            Voltage = 0;
            Current = 0;
            TimeDelayBeforePowerOn = 0;
            TimeDelayAfterPowerOn = 0;
            VoltageLimitLow = -0.01;
            VoltageLimitHigh = 0.01;
            CurrentLimitLow = -0.01;
            CurrentLimitHigh = 0.01;

            // Validation rules
            Rules.Add(() => TimeDelayBeforePowerOn >= 0,
                "Time delay must be greater than or equal to zero", nameof(TimeDelayBeforePowerOn));
            Rules.Add(() => TimeDelayAfterPowerOn >= 0,
                "Time delay must be greater than or equal to zero", nameof(TimeDelayAfterPowerOn));
            Rules.Add(() => VoltageLimitLow <= VoltageLimitHigh,
                "Lower limit cannot be greater than upper limit", nameof(VoltageLimitLow));
            Rules.Add(() => VoltageLimitLow <= VoltageLimitHigh,
                "Lower limit cannot be greater than upper limit", nameof(VoltageLimitHigh));
            Rules.Add(() => CurrentLimitLow <= CurrentLimitHigh,
                "Lower limit cannot be greater than upper limit", nameof(CurrentLimitLow));
            Rules.Add(() => CurrentLimitLow <= CurrentLimitHigh,
                "Lower limit cannot be greater than upper limit", nameof(CurrentLimitHigh));
        }

        public override void Run()
        {
            try
            {
                if (!DcPwr.IsConnected)
                    throw new InvalidOperationException("Power Supply not connected or initialized!");

                DcPwr.SetOutputState(EState.Off);

                DcPwr.SetCurrent(Current);
                DcPwr.SetVoltage(Voltage);

                TapThread.Sleep(TimeSpan.FromSeconds(TimeDelayBeforePowerOn));

                DcPwr.SetOutputState(EState.On);

                TapThread.Sleep(TimeSpan.FromSeconds(TimeDelayAfterPowerOn));

                var measuredVoltage = DcPwr.MeasureVoltage();
                var measuredCurrent = DcPwr.MeasureCurrent();

                if (measuredVoltage < VoltageLimitLow || measuredVoltage > VoltageLimitHigh)
                    throw new InvalidOperationException(
                        $"The measured voltage of {Math.Round(measuredVoltage, 3)} is not " +
                        $"within the expected limits of {VoltageLimitLow} to {VoltageLimitHigh}");

                if (measuredCurrent < CurrentLimitLow || measuredCurrent > CurrentLimitHigh)
                    throw new InvalidOperationException(
                        $"The measured voltage of {Math.Round(measuredCurrent, 3)} is not " +
                        $"within the expected limits of {CurrentLimitLow} to {CurrentLimitHigh}");

                UpgradeVerdict(Verdict.Pass);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }
    }
}