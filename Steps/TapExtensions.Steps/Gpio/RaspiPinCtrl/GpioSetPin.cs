using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Steps.Gpio.RaspiPinCtrl
{
    [Display("GpioSetPin",
        Groups: new[] { "TapExtensions", "Steps", "Gpio", "RaspiPinCtrl" })]
    public class GpioSetPin : RaspiPinCtrlBase
    {
        [Display("Pin Number", Order: 2)] public int PinNumber { get; set; }

        [Display("Pin Direction", Order: 3)] public EDirection Direction { get; set; }

        [Display("Pin Pull", Order: 4)] public EPull Pull { get; set; }

        [EnabledIf(nameof(Direction), EDirection.Output)]
        [Display("Pin Output Drive", Order: 5)]
        public EDrive Drive { get; set; }

        public GpioSetPin()
        {
            Rules.Add(() => PinNumber >= 2 && PinNumber <= 27,
                "Pin number must be between 2 and 27", nameof(PinNumber));
        }

        public override void Run()
        {
            try
            {
                ThrowOnValidationError(true);

                if (Direction == EDirection.Output)
                    SetPin(PinNumber, Direction, Pull, Drive);
                else
                    SetPin(PinNumber, Direction, Pull);

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