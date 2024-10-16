using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Steps.Gpio
{
    [Display("RaspiGpioSetPin", Groups: new[] { "TapExtensions", "Steps", "Gpio" })]
    public class RaspiGpioSetPin : RaspiGpio
    {
        [Display("Pin", Order: 2)] public ERaspiGpio Pin { get; set; } = ERaspiGpio.GPIO_05_PINHDR_29;

        [Display("OutputDrive", Order: 5)] public EDrive Drive { get; set; } = EDrive.DriveHigh;

        public override void Run()
        {
            try
            {
                // SetPinDrive((int)Pin, Drive);
                var pin = (int)Pin;
                var drive = EnumToString(Drive);
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