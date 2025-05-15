using System;
using System.Collections.Generic;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Steps.Gpio.RaspiGpio
{
    [Display("GpioSetPins",
        Groups: new[] { "TapExtensions", "Steps", "Gpio", "RaspiGpio" })]
    public class GpioSetPins : TestStep
    {
        [Display("Gpio", Order: 1)] public IGpio Gpio { get; set; }

        public class Config : ValidatingObject
        {
            [Display("Pin Number", Order: 2)] public int PinNumber { get; set; }

            [Display("Direction", Order: 3)] public EDirection Direction { get; set; }

            [Display("Pull", Order: 4)] public EPull Pull { get; set; }

            [EnabledIf(nameof(Direction), EDirection.Output)]
            [Display("Output Drive", Order: 5)]
            public EDrive Drive { get; set; }

            public Config()
            {
                Rules.Add(() => PinNumber >= 2 && PinNumber <= 27,
                    "Raspberry Pi's GPIO number must be between 2 and 27",
                    nameof(PinNumber));
            }
        }

        [Display("List of Pins", Order: 6)]
        public List<Config> ListOfPins { get; set; } = new List<Config>
        {
            new Config
            {
                PinNumber = 5,
                Direction = EDirection.Input,
                Pull = EPull.PullNone
            },
            new Config
            {
                PinNumber = 6,
                Direction = EDirection.Input,
                Pull = EPull.PullNone
            }
        };

        public override void Run()
        {
            try
            {
                foreach (var config in ListOfPins)
                {
                    if (config.Direction == EDirection.Output)
                        Gpio.SetPin(config.PinNumber, config.Direction, config.Pull, config.Drive);
                    else
                        Gpio.SetPin(config.PinNumber, config.Direction, config.Pull);
                }

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