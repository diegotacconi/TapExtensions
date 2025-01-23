using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Steps.Gpio
{
    [Display("GpioSetPinDirection", Groups: new[] { "TapExtensions", "Steps", "Gpio" })]
    public class GpioSetPinDirection : TestStep
    {
        [Display("Gpio", Order: 1)] public IGpio Gpio { get; set; }

        [Display("Pin Number", Order: 2)] public int PinNumber { get; set; }

        [Display("Pin Direction", Order: 3)] public EDirection Direction { get; set; }

        public override void Run()
        {
            try
            {
                Gpio.SetPinDirection(PinNumber, Direction);
                Log.Debug($"Set pin {PinNumber} as {Direction}");
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