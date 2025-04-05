﻿using System;
using OpenTap;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c.Devices
{
    [Display("Ads1015Measure",
        Groups: new[] { "TapExtensions", "Steps", "I2c", "Devices" })]
    public class Ads1015Measure : TestStep
    {
        [Display("I2C Adapter", Order: 1)] public II2C I2CAdapter { get; set; }

        [Display("Device Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort DeviceAddress { get; set; } = 0x48;

        [Display("Input Multiplexer", Order: 3)]
        public Ads1015.EInputMux InputMux { get; set; } = Ads1015.EInputMux.Ain0;

        [Display("Gain Precision", Order: 4)]
        public Ads1015.EGainPrecision GainPrecision { get; set; } = Ads1015.EGainPrecision.Range2;

        public override void Run()
        {
            try
            {
                var ads1015 = new Ads1015(I2CAdapter, DeviceAddress);
                ads1015.ConfigAndMeasure(InputMux, GainPrecision);
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