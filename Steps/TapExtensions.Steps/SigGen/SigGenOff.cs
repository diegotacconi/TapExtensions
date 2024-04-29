using System;
using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.SigGen;

namespace TapExtensions.Steps.SigGen
{
    [Display("SigGenOff",
        Groups: new[] { "TapExtensions", "Steps", "SigGen" })]
    public class SigGenOff : TestStep
    {
        [Display("SigGen", Group: "Instruments",
            Description: "RF Signal Generator instrument interface")]
        public ISigGen SigGen { get; set; }

        public override void Run()
        {
            try
            {
                SigGen.SetRfOutputState(EState.Off);
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