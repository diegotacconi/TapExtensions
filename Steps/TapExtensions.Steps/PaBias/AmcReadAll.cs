using System;
using OpenTap;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.PaBias
{
    [Display("AmcReadAll",
        Groups: new[] { "TapExtensions", "Steps", "PaBias" })]
    public class AmcReadAll : TestStep
    {
        [Display("I2C Adapter", Order: 1)] public II2C I2CAdapter { get; set; }

        [Display("Device Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort DeviceAddress { get; set; } = 0x44;

        public override void Run()
        {
            try
            {
                var amc = new Amc(I2CAdapter, DeviceAddress);
                var lines = amc.ReadAll();
                foreach (var line in lines)
                    Log.Debug(line);

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