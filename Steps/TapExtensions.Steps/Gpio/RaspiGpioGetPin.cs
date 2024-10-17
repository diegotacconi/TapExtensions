using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

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
                var measuredLevel = GetPinLevel((int)Pin);
                if (measuredLevel != ExpectedLevel)
                    throw new InvalidOperationException(
                        $"Measured level of '{measuredLevel}' is not equal to the expected level of '{ExpectedLevel}'");

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