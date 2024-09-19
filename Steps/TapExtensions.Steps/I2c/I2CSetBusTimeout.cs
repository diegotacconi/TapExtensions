using System;
using OpenTap;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c
{
    [Display("I2CSetBusTimeout",
        Groups: new[] { "TapExtensions", "Steps", "I2c" })]
    public class I2CSetBusTimeout : TestStep
    {
        [Display("I2C Adapter", Order: 1)]
        public II2C I2CAdapter { get; set; }

        [Display("Timeout", Order: 4)]
        [Unit("ms")]
        public ushort TimeoutMs { get; set; } = 0;

        public override void Run()
        {
            try
            {
                I2CAdapter.SetBusTimeOutInMs(TimeoutMs);
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