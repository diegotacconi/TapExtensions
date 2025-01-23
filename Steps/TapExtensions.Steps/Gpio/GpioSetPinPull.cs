using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Steps.Gpio
{
    [Display("GpioSetPinPull", Groups: new[] { "TapExtensions", "Steps", "Gpio" })]
    public class GpioSetPinPull : TestStep
    {
        [Display("Gpio", Order: 1)] public IGpio Gpio { get; set; }

        [Display("Pin Number", Order: 2)] public int PinNumber { get; set; }

        [Display("Pin Pull", Order: 3)] public EPull Pull { get; set; }

        public override void Run()
        {
            try
            {
                Gpio.SetPinPull(PinNumber, Pull);
                Log.Debug($"Set pin {PinNumber} to {Pull}");
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