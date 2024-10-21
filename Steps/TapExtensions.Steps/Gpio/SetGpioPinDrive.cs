using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Steps.Gpio
{
    [Display("SetGpioPinDrive", Groups: new[] { "TapExtensions", "Steps", "Gpio" })]
    public class SetGpioPinDrive : TestStep
    {
        [Display("Gpio", Order: 1)] public IGpio Gpio { get; set; }

        [Display("Pin Number", Order: 2)] public int PinNumber { get; set; }

        [Display("Pin Drive", Order: 3)] public EDrive Drive { get; set; }

        public override void Run()
        {
            try
            {
                Gpio.SetPinDrive(PinNumber, Drive);
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