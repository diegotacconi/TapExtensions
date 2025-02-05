using System;
using OpenTap;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.PaBias
{
    [Display("AmcMeasureTemperature",
        Groups: new[] { "TapExtensions", "Steps", "PaBias" })]
    public class AmcMeasureTemperature : TestStep
    {
        [Display("I2C Adapter", Order: 1)] public II2C I2CAdapter { get; set; }

        [Display("Device Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort DeviceAddress { get; set; } = 0x44;

        [Display("Low Limit", Order: 3, Group: "Limits")]
        [Unit("C")]
        public double LowLimit { get; set; } = 10;

        [Display("High Limit", Order: 4, Group: "Limits")]
        [Unit("C")]
        public double HighLimit { get; set; } = 80;

        public AmcMeasureTemperature()
        {
            // Validation rules
            Rules.Add(() => LowLimit <= HighLimit,
                "Lower limit cannot be greater than upper limit", nameof(LowLimit));
            Rules.Add(() => LowLimit <= HighLimit,
                "Lower limit cannot be greater than upper limit", nameof(HighLimit));
        }

        public override void Run()
        {
            try
            {
                var amc = new Amc(I2CAdapter, DeviceAddress);
                var temperature = amc.MeasureTemperature();
                Log.Debug($"Temperature = {Math.Round(temperature, 3)} C");
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