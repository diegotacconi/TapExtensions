using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Steps.Gpio.RaspiPinCtrl
{
    [Display("RaspiSshGpioGetPin",
        Groups: new[] { "TapExtensions", "Steps", "Gpio", "RaspiPinCtrl" })]
    public class RaspiGpioGetPin : RaspiGpio
    {
        [Display("Pin Number", Order: 2)] public ERaspiGpio Pin { get; set; }

        [Display("Expected Pin Level", Order: 3)]
        public ELevel ExpectedLevel { get; set; }

        public override void Run()
        {
            try
            {
                var (_, _, measuredLevel) = GetPin((int)Pin);
                if (measuredLevel != ExpectedLevel)
                    throw new InvalidOperationException(
                        $"Pin {Pin} measured an input level of {measuredLevel}, " +
                        $"which is not equal to the expected level of {ExpectedLevel}.");

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