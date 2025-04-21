using System;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.SigGen;

namespace TapExtensions.Steps.SigGen
{
    [Display("SigGenOn",
        Groups: new[] { "TapExtensions", "Steps", "SigGen" })]
    public class SigGenOn : TestStep
    {
        [Display("SigGen", Order: 1, Group: "Instruments",
            Description: "RF Signal Generator instrument interface")]
        public ISigGen SigGen { get; set; }

        [Display("Set Frequency", Order: 2, Group: "Parameters")]
        [Unit("MHz")]
        public Enabled<double> FrequencyMhz { get; set; } = new Enabled<double> { IsEnabled = true, Value = 1000 };

        [Display("Set Amplitude", Order: 3, Group: "Parameters")]
        [Unit("dBm")]
        public Enabled<double> AmplitudeDbm { get; set; } = new Enabled<double> { IsEnabled = true, Value = 0 };

        public override void Run()
        {
            try
            {
                if (FrequencyMhz.IsEnabled)
                    SigGen.SetFrequency(FrequencyMhz.Value);

                if (AmplitudeDbm.IsEnabled)
                    SigGen.SetOutputLevel(AmplitudeDbm.Value);

                SigGen.SetRfOutputState(EState.On);
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