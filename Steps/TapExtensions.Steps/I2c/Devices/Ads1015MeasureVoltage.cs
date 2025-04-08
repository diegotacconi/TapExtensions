using System;
using OpenTap;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c.Devices
{
    [Display("Ads1015MeasureVoltage",
        Groups: new[] { "TapExtensions", "Steps", "I2c", "Devices" })]
    public class Ads1015MeasureVoltage : TestStep
    {
        [Display("I2C Adapter", Order: 1)] public II2C I2CAdapter { get; set; }

        [Display("Device Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort DeviceAddress { get; set; } = 0x48;

        [Display("Input Multiplexer", Order: 3)]
        public Ads1015.EInputMux InputMux { get; set; } = Ads1015.EInputMux.Ain0;

        [Display("Gain Resolution", Order: 4)]
        public Ads1015.EGainResolution GainResolution { get; set; } = Ads1015.EGainResolution.Range2;

        public override void Run()
        {
            try
            {
                var ads1015 = new Ads1015(I2CAdapter, DeviceAddress);
                ads1015.MeasureVoltage(InputMux, GainResolution);
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