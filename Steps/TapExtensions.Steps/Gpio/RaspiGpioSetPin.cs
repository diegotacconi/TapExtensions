using System;
using OpenTap;

namespace TapExtensions.Steps.Gpio
{
    [Display("RaspiGpioSetPin", Groups: new[] { "TapExtensions", "Steps", "Gpio" })]
    public class RaspiGpioSetPin : RaspiGpio
    {
        [Display("Pin", Order: 2)] public ERaspiGpio Pin { get; set; } = ERaspiGpio.GPIO_05_PINHDR_29;

        [Display("OutputDrive", Order: 5)] public EOutputDrive OutputDrive { get; set; } = EOutputDrive.DriveHigh;

        public override void Run()
        {
            try
            {
                var pin = (int)Pin;
                var drive = GetShortCommand(OutputDrive.ToString());
                var cmd = $"sudo pinctrl set {pin} {drive}";
                Log.Debug(cmd);
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