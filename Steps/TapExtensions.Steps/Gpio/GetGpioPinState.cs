using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Steps.Gpio
{
    [Display("GetGpioPinState", Groups: new[] { "TapExtensions", "Steps", "Gpio" })]
    public class GetGpioPinState : TestStep
    {
        [Display("Gpio", Order: 1)] public IGpio Gpio { get; set; }

        [Display("Pin Number", Order: 2)] public int PinNumber { get; set; }

        [Display("Expected Pin State", Order: 3)]
        public EPinState ExpectedPinState { get; set; }

        public override void Run()
        {
            try
            {
                var measuredPinState = Gpio.GetPinState(PinNumber);
                UpgradeVerdict(measuredPinState == ExpectedPinState ? Verdict.Pass : Verdict.Fail);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }
    }
}