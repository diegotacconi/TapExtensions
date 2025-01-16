using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c.Devices
{
    [Display("Tca6416AReadAllRegisters",
        Groups: new[] { "TapExtensions", "Steps", "I2c", "Devices" })]
    public class Tca6416AReadAllRegisters : TestStep
    {
        [Display("Aardvark I2C Adapter", Order: 1)]
        public II2C I2CAdapter { get; set; }

        [Display("Device Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort DeviceAddress { get; set; } = 0x20;

        public override void Run()
        {
            try
            {
                var tca6416A = new Tca6416A(I2CAdapter, DeviceAddress);
                tca6416A.ReadAllRegisters();
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