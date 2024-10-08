﻿using System;
using OpenTap;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c
{
    [Display("I2CWriteRegister",
        Groups: new[] { "TapExtensions", "Steps", "I2c" })]
    public class I2CWriteRegister : TestStep
    {
        [Display("I2C Adapter", Order: 1)]
        public II2C I2CAdapter { get; set; }

        [Display("Device Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort DeviceAddress { get; set; } = 0x48;

        [Display("Register Address", Order: 3)]
        [Unit("Hex", StringFormat: "X2")]
        public byte RegisterAddress { get; set; } = 0x00;

        [Display("Command", Order: 5)]
        [Unit("Hex", StringFormat: "X2")]
        public byte Command { get; set; } = 0x00;

        public override void Run()
        {
            try
            {
                var regAddress = new[] { RegisterAddress };
                var command = new[] { Command };
                I2CAdapter.Write(DeviceAddress, regAddress, command);
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