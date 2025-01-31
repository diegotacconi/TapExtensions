using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Steps.Gpio.Raspi
{
    [Display("RaspiGpioSetPinDrive",
        Groups: new[] { "TapExtensions", "Steps", "Gpio", "Raspi" })]
    public class RaspiGpioSetPinDrive : TestStep
    {
        [Display("Gpio", Order: 1)] public IGpio Gpio { get; set; }

        [Display("Pin Number", Order: 2)] public ERaspiGpio PinNumber { get; set; }

        [Display("Pin Output Drive", Order: 3)]
        public EDrive Drive { get; set; }

        public override void Run()
        {
            try
            {
                Gpio.SetPinDrive((int)PinNumber, Drive);
                Log.Debug($"Set pin {PinNumber} to {Drive}");
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