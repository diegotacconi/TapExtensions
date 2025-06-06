using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Steps.Gpio.RaspiPinCtrl
{
    [Display("GpioGetPin",
        Groups: new[] { "TapExtensions", "Steps", "Gpio", "RaspiPinCtrl" })]
    public class GpioGetPin : RaspiPinCtrlBase
    {
        [Display("Pin Number", Order: 2)] public int PinNumber { get; set; }

        [Display("Expected Pin Level", Order: 3)]
        public ELevel ExpectedLevel { get; set; }

        public GpioGetPin()
        {
            Rules.Add(() => PinNumber >= 2 && PinNumber <= 27,
                "Pin number must be between 2 and 27", nameof(PinNumber));
        }

        public override void Run()
        {
            try
            {
                ThrowOnValidationError(true);

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