using System;
using System.Collections.Generic;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Steps.Gpio
{
    [Display("SetMultipleGpioPins", Groups: new[] { "TapExtensions", "Steps", "Gpio" })]
    public class SetMultipleGpioPins : TestStep
    {
        [Display("Gpio", Order: 1)] public IGpio Gpio { get; set; }

        public class Config : ValidatingObject
        {
            [Display("Pin Number", Order: 2)] public int Pin { get; set; }

            [Display("Direction", Order: 3)] public EDirection Direction { get; set; }

            [Display("Pull", Order: 4)] public EPull Pull { get; set; }

            [EnabledIf(nameof(Direction), EDirection.Output)]
            [Display("Drive", Order: 5)]
            public EDrive Drive { get; set; }
        }

        [Display("List of Pins", Order: 6)]
        public List<Config> ListOfPins { get; set; } = new List<Config>
        {
            new Config { Pin = 5, Direction = EDirection.Output, Pull = EPull.PullUp, Drive = EDrive.DriveHigh },
            new Config { Pin = 6, Direction = EDirection.Input, Pull = EPull.PullUp }
        };

        public override void Run()
        {
            try
            {
                foreach (var config in ListOfPins)
                {
                    Gpio.SetPinDirection(config.Pin, config.Direction);
                    Gpio.SetPinPull(config.Pin, config.Pull);

                    if (config.Direction == EDirection.Output)
                        Gpio.SetPinDrive(config.Pin, config.Drive);
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