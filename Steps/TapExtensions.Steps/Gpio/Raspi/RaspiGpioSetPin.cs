using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Steps.Gpio.Raspi
{
    [Display("RaspiGpioSetPin",
        Groups: new[] { "TapExtensions", "Steps", "Gpio", "Raspi" })]
    public class RaspiGpioSetPin : TestStep
    {
        [Display("Gpio", Order: 1)] public IGpio Gpio { get; set; }

        [Display("Pin Number", Order: 2)] public ERaspiGpio Pin { get; set; }

        [Display("Pin Direction", Order: 3)] public EDirection Direction { get; set; }

        [Display("Pin Pull", Order: 4)] public EPull Pull { get; set; }

        [EnabledIf(nameof(Direction), EDirection.Output)]
        [Display("Pin Output Drive", Order: 5)]
        public EDrive Drive { get; set; }

        public override void Run()
        {
            try
            {
                if (Direction == EDirection.Output)
                    Gpio.SetPin((int)Pin, Direction, Pull, Drive);
                else
                    Gpio.SetPin((int)Pin, Direction, Pull);

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