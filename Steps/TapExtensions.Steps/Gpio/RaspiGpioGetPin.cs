using System;
using OpenTap;

namespace TapExtensions.Steps.Gpio
{
    [Display("RaspiGpioGetPin", Groups: new[] { "TapExtensions", "Steps", "Gpio" })]
    public class RaspiGpioGetPin : RaspiGpio
    {
        [Display("Pin", Order: 2)] public ERaspiGpio Pin { get; set; } = ERaspiGpio.GPIO_06_PINHDR_31;

        [Display("Expected Level", Order: 6)] public ELevel ExpectedLevel { get; set; } = ELevel.High;

        public override void Run()
        {
            try
            {
                var pin = (int)Pin;
                var expectedLevel = GetShortCommand(ExpectedLevel.ToString());
                var measuredLevel = GetShortCommand(ExpectedLevel.ToString());
                var cmd = $"sudo pinctrl get {pin} ==> (measured = '{measuredLevel}', expected = '{expectedLevel}')";
                Log.Debug(cmd);
                UpgradeVerdict(measuredLevel == expectedLevel ? Verdict.Pass : Verdict.Fail);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }
    }
}