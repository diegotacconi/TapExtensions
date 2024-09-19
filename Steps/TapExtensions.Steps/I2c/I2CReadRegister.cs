using System;
using OpenTap;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c
{
    [Display("I2CReadRegister",
        Groups: new[] { "TapExtensions", "Steps", "I2c" })]
    public class I2CReadRegister : TestStep
    {
        [Display("I2C Adapter", Order: 1)]
        public II2C I2CAdapter { get; set; }

        [Display("Device Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort DeviceAddress { get; set; } = 0x48;

        [Display("Register Address", Order: 3)]
        [Unit("Hex", StringFormat: "X2")]
        public byte RegisterAddress { get; set; } = 0x00;

        [Display("Number of Data Bytes", Order: 4)]
        public ushort NumberOfDataBytes { get; set; } = 2;

        public override void Run()
        {
            try
            {
                var regAddress = new[] { RegisterAddress };
                I2CAdapter.Read(DeviceAddress, NumberOfDataBytes, regAddress);
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