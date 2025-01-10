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
                var tca6416A = new Tca6416A
                {
                    I2CAdapter = I2CAdapter,
                    DeviceAddress = DeviceAddress
                };

                // Debug start
                var registers = tca6416A.ReadRegisters(out var lvl, out var drive, out var polarity, out var dir);
                var binaryString = string.Join(" ", registers.Select(x => Convert.ToString(x, 2).PadLeft(8, '0')));
                Log.Debug($"Registers = {binaryString}");
                Log.Debug($"Lvl       = {Convert.ToString(lvl, 2).PadLeft(16, '0')}");
                Log.Debug($"Drive     = {Convert.ToString(drive, 2).PadLeft(16, '0')}");
                Log.Debug($"Polarity  = {Convert.ToString(polarity, 2).PadLeft(16, '0')}");
                Log.Debug($"Dir       = {Convert.ToString(dir, 2).PadLeft(16, '0')}");
                // Debug end

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