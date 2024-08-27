using System;
using OpenTap;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c
{
    [Display("I2CMasterRegRead",
        Groups: new[] { "TapExtensions", "Steps", "I2c" })]
    public class I2CMasterRegRead : TestStep
    {
        [Display("I2C Adapter", Order: 1)]
        public II2C I2CAdapter { get; set; }

        [Display("Slave Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort SlaveAddress { get; set; }

        [Display("Register Address", Order: 3)]
        [Unit("Hex", StringFormat: "X2")]
        public byte RegisterAddress { get; set; }

        [Display("Number of Data Bytes", Order: 4)]
        public ushort NumberOfDataBytes { get; set; }

        public I2CMasterRegRead()
        {
            SlaveAddress = 0x48;
            RegisterAddress = 0x00;
            NumberOfDataBytes = 2;
        }

        public override void Run()
        {
            try
            {
                var regAddress = new[] { RegisterAddress };
                I2CAdapter.Read(SlaveAddress, NumberOfDataBytes, regAddress);
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