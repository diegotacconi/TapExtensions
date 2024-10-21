using System;
using OpenTap;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c.Devices
{
    [Display("Mcp23017Control",
        Groups: new[] { "TapExtensions", "Steps", "I2c", "Devices" })]
    public class Mcp23017Control : TestStep
    {
        [Display("I2C Adapter", Order: 1)] public II2C I2CAdapter { get; set; }

        [Display("Device Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort DeviceAddress { get; set; } = 0x20;

        public override void Run()
        {
            try
            {
                var mcp23017 = new Mcp23017(I2CAdapter, DeviceAddress);
                var registers = mcp23017.ReadRegisters(out var direction, out var polarity, out var pull, out var level,
                    out var drive);

                // Display register values
                // var binaryString = string.Join(" ", registers.Select(x => Convert.ToString(x, 2).PadLeft(8, '0')));
                // Log.Debug($"Registers = {binaryString}");
                Log.Debug($"Direction = {Convert.ToString(direction, 2).PadLeft(16, '0')}");
                Log.Debug($"Polarity  = {Convert.ToString(polarity, 2).PadLeft(16, '0')}");
                Log.Debug($"Pull      = {Convert.ToString(pull, 2).PadLeft(16, '0')}");
                Log.Debug($"Level     = {Convert.ToString(level, 2).PadLeft(16, '0')}");
                Log.Debug($"Drive     = {Convert.ToString(drive, 2).PadLeft(16, '0')}");

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