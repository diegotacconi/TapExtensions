// Example of a parent that has certain resources available (the DUT and Instrument).
// Children of this parent may "reach up" and use those resources.
// This allows resources to be defined once, for a set of sibling children.

using System;
using OpenTap;

namespace TapExtensions.Steps.ParentChild
{
    [Display("ParentWithResources",
        Groups: new[] { "TapExtensions", "Steps", "ParentChild" })]
    [AllowChildrenOfType(typeof(ChildSeekingResources))]
    public class ParentWithResources : TestStep
    {
        public Dut SomeDut { get; set; }
        public Instrument SomeInstrument { get; set; }
        public string SomeString { get; set; }

        public override void Run()
        {
            try
            {
                Log.Debug($"Parent={Name}, " +
                          $"Dut={SomeDut.Name}, " +
                          $"Instrument={SomeInstrument.Name}, " +
                          $"String={SomeString}.");

                RunChildSteps();

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