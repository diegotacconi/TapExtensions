using System;
using OpenTap;

namespace TapExtensions.Steps.Gpio.RaspiSsh.RaspiGpio
{
    [Display("GpioGetPin",
        Groups: new[] { "TapExtensions", "Steps", "Gpio", "RaspiSsh", "RaspiGpio" })]
    public class GpioGetPin : RaspiSshRaspiGpio
    {
        [Display("Pin Number", Order: 2)] public int PinNumber { get; set; }

        [Display("Expected Pin Level", Order: 3)]
        public ELevel ExpectedLevel { get; set; }

        public override void Run()
        {
            try
            {
                var (_, _, measuredLevel) = GetPin(PinNumber);
                if (measuredLevel != ExpectedLevel)
                    throw new InvalidOperationException(
                        $"Pin {PinNumber} measured an input level of {measuredLevel}, " +
                        $"which is not equal to the expected level of {ExpectedLevel}.");

                Log.Debug($"Pin {PinNumber} measured {measuredLevel}");
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