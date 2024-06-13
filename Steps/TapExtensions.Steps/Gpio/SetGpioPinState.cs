using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Steps.Gpio
{
    [Display("SetGpioPinState", Groups: new[] { "TapExtensions", "Steps", "Gpio" })]
    public class SetGpioPinState : TestStep
    {
        [Display("Gpio", Order: 1)] public IGpio Gpio { get; set; }

        [Display("Pin Number", Order: 2)] public int PinNumber { get; set; }

        [Display("Pin State", Order: 3)] public EPinState PinState { get; set; }

        public override void Run()
        {
            try
            {
                Gpio.SetPinState(PinNumber, PinState);
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