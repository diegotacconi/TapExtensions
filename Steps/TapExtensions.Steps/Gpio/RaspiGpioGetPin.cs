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
                var pin = (int)Pin;

                // var measuredLevel = GetPinLevel(Pin);
                var cmd = $"sudo pinctrl get {pin}";
                Log.Debug(cmd);
                var response = " 6: ip    pu | hi // GPIO6 = input";
                Log.Debug(response);
                var measuredLevel = (ELevel)ParseLevel(response);

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