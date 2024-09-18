using System;
using OpenTap;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c
{
    [Display("I2CRead",
        Groups: new[] { "TapExtensions", "Steps", "I2c" })]
    public class I2CRead : TestStep
    {
        [Display("I2C Adapter", Order: 1)]
        public II2C I2CAdapter { get; set; }

        [Display("Device Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort DeviceAddress { get; set; } = 0x48;

        [Display("Number of Data Bytes", Order: 4)]
        public ushort NumberOfDataBytes { get; set; } = 2;

        public override void Run()
        {
            try
            {
                I2CAdapter.Read(DeviceAddress, NumberOfDataBytes);
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