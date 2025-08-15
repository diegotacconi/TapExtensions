// Example of a parent that has certain resources available (the DUT and Instrument).
// Children of this parent may "reach up" and use those resources.
// This allows resources to be defined once, for a set of sibling children.

using System;
using OpenTap;

namespace TapExtensions.Steps.ParentChild
{
    [Display("ParentWithResources",
        Groups: new[] { "TapExtensions", "Steps", "ParentChild" })]
    [AllowAnyChild]
    public class ParentWithResources : TestStep
    {
        #region Settings

        public Dut SomeDut { get; set; }
        public Instrument SomeInstrument { get; set; }
        public string SomeString { get; set; } = "ABC";

        #endregion

        public override void Run()
        {
            try
            {
                // Show values before running child steps
                Log.Debug($"Parent={Name}(before), " +
                          $"Dut={SomeDut.Name}, " +
                          $"Instrument={SomeInstrument.Name}, " +
                          $"String={SomeString}.");

                RunChildSteps();

                // Show values after running child steps
                Log.Debug($"Parent={Name}(after), " +
                          $"Dut={SomeDut.Name}, " +
                          $"Instrument={SomeInstrument.Name}, " +
                          $"String={SomeString}.");

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