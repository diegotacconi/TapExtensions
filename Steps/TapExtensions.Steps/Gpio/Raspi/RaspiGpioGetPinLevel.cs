﻿using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Steps.Gpio.Raspi
{
    [Display("RaspiGpioGetPinLevel",
        Groups: new[] { "TapExtensions", "Steps", "Gpio", "Raspi" })]
    public class RaspiGpioGetPinLevel : TestStep
    {
        [Display("Gpio", Order: 1)] public IGpio Gpio { get; set; }

        [Display("Pin Number", Order: 2)] public ERaspiGpio PinNumber { get; set; }

        [Display("Expected Pin Level", Order: 3)]
        public ELevel ExpectedLevel { get; set; }

        public override void Run()
        {
            try
            {
                var measuredLevel = Gpio.GetPinLevel((int)PinNumber);
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