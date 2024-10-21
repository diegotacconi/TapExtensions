using System;
using OpenTap;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c.Devices
{
    [Display("Tmp102ReadTemperature",
        Groups: new[] { "TapExtensions", "Steps", "I2c", "Devices" })]
    public class Tmp102ReadTemperature : TestStep
    {
        [Display("I2C Adapter", Order: 1)] public II2C I2CAdapter { get; set; }

        [Display("Device Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort DeviceAddress { get; set; } = 0x48;

        public override void Run()
        {
            try
            {
                var tmp102 = new Tmp102(I2CAdapter, DeviceAddress);
                var temperature = tmp102.ReadTemperature();
                Log.Debug($"TMP102 temperature = {temperature} C");
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