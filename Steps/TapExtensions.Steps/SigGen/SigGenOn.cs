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
        [Display("SigGen", Group: "Instruments",
            Description: "RF Signal Generator instrument interface")]
        public ISigGen SigGen { get; set; }

        [Display("Set Frequency", Order: 2, Group: "Parameters")]
        [Unit("MHz")]
        public double FrequencyMhz { get; set; } = 1000;

        public override void Run()
        {
            try
            {
                SigGen.SetFrequency(FrequencyMhz);
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