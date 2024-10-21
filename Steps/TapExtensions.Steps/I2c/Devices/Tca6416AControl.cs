using System;
using System.Linq;
using OpenTap;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c.Devices
{
    [Display("Tca6416AControl",
        Groups: new[] { "TapExtensions", "Steps", "I2c", "Devices" })]
    public class Tca6416AControl : TestStep
    {
        [Display("I2C Adapter", Order: 1)] public II2C I2CAdapter { get; set; }

        [Display("Device Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort DeviceAddress { get; set; } = 0x20;

        public override void Run()
        {
            try
            {
                var tca6416A = new Tca6416A(I2CAdapter, DeviceAddress);
                var registers = tca6416A.ReadRegisters(out _, out _, out _, out _);
                var binaryString = string.Join(" ", registers.Select(x => Convert.ToString(x, 2).PadLeft(8, '0')));
                Log.Debug($"TCA6416A Registers = {binaryString}");
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