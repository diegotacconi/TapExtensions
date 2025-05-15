using System;
using OpenTap;

namespace TapExtensions.Steps.Gpio.RaspiSsh.RaspiGpio
{
    [Display("GpioSetPin",
        Groups: new[] { "TapExtensions", "Steps", "Gpio", "RaspiSsh", "RaspiGpio" })]
    public class GpioSetPin : RaspiSshRaspiGpio
    {
        [Display("Pin Number", Order: 2)] public int PinNumber { get; set; }

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