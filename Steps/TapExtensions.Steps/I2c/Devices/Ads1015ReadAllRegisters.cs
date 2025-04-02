using System;
using OpenTap;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c.Devices
{
    [Display("Ads1015ReadAllRegisters",
        Groups: new[] { "TapExtensions", "Steps", "I2c", "Devices" })]
    public class Ads1015ReadAllRegisters : TestStep
    {
        [Display("I2C Adapter", Order: 1)] public II2C I2CAdapter { get; set; }

        [Display("Device Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort DeviceAddress { get; set; } = 0x48;

        public override void Run()
        {
            try
            {
                var ads1015 = new Ads1015(I2CAdapter, DeviceAddress);
                ads1015.ReadAllRegisters();
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