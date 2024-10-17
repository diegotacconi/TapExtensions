using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Steps.Gpio
{
    [Display("RaspiGpioConfigPin", Groups: new[] { "TapExtensions", "Steps", "Gpio" })]
    public class RaspiGpioConfigPin : RaspiGpio
    {
        [Display("Pin", Order: 2)] public ERaspiGpio Pin { get; set; }

        [Display("Direction", Order: 3)] public EDirection Direction { get; set; }

        [Display("Pull", Order: 4)] public EPull Pull { get; set; }

        public override void Run()
        {
            try
            {
                SetPinDirection((int)Pin, Direction);
                SetPinPull((int)Pin, Pull);
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