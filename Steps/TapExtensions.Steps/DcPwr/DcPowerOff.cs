using System;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.DcPwr;

namespace TapExtensions.Steps.DcPwr
{
    [Display("DcPowerOff",
        Groups: new[] { "TapExtensions", "Steps", "DcPwr" })]
    public class DcPowerOff : TestStep
    {
        #region Settings

        [Display("DcPwr", Order: 1,
            Description: "DC Power Supply instrument interface")]
        public IDcPwr DcPwr { get; set; }

        [Display("Measure Voltage And Current", Order: 2,
            Description: "Measure before turning off the power")]
        public bool MeasureVoltageAndCurrent { get; set; } = false;

        #endregion

        public override void Run()
        {
            try
            {
                if (!DcPwr.IsConnected)
                    throw new InvalidOperationException($"Cannot connect to {DcPwr}.");

                if (MeasureVoltageAndCurrent)
                {
                    var measuredVoltage = DcPwr.MeasureVoltage();
                    var measuredCurrent = DcPwr.MeasureCurrent();
                    Log.Debug("Before turning off the power, " +
                              $"the voltage is {Math.Round(measuredVoltage, 3)} V, and " +
                              $"the current is {Math.Round(measuredCurrent, 3)} A.");
                }

                DcPwr.SetOutputState(EState.Off);

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