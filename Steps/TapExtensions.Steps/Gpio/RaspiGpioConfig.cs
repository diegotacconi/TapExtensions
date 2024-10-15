﻿using System;
using System.Collections.Generic;
using OpenTap;

namespace TapExtensions.Steps.Gpio
{
    [Display("RaspiGpioConfig", Groups: new[] { "TapExtensions", "Steps", "Gpio" })]
    public class RaspiGpioConfig : RaspiGpio
    {
        public class Config : ValidatingObject
        {
            [Display("Pin", Order: 2)] public ERaspiGpio Pin { get; set; }

            [Display("Direction", Order: 3)] public EDirection Direction { get; set; }

            [Display("Pull", Order: 4)] public EPull Pull { get; set; }
        }

        [Display("Configs", Order: 5)]
        public List<Config> Configs { get; set; } = new List<Config>
        {
            new Config { Pin = ERaspiGpio.GPIO_05_PINHDR_29, Direction = EDirection.Output, Pull = EPull.PullUp },
            new Config { Pin = ERaspiGpio.GPIO_06_PINHDR_31, Direction = EDirection.Input, Pull = EPull.PullUp }
        };

        public override void Run()
        {
            try
            {
                foreach (var config in Configs)
                {
                    var pin = (int)config.Pin;
                    var direction = GetShortCommand(config.Direction.ToString());
                    var pull = GetShortCommand(config.Pull.ToString());
                    var cmd = $"sudo pinctrl set {pin} {direction} {pull}";
                    Log.Debug(cmd);
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