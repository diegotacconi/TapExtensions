// This example shows how a child can get a parent with of a certain type, and then
// recover some properties from that parent.

using System;
using OpenTap;

namespace TapExtensions.Steps.ParentChild
{
    [Display("ChildSeekingResources1",
        Groups: new[] { "TapExtensions", "Steps", "ParentChild" })]
    [AllowAsChildIn(typeof(ParentWithResources))]
    public class ChildSeekingResources1 : TestStep
    {
        #region Settings

        private Dut _parentsDut;
        private Instrument _parentsInstrument;
        private string _parentsString;

        #endregion

        private void GetParentResources()
        {
            // Get values
            _parentsDut = GetParent<ParentWithResources>().SomeDut;
            _parentsInstrument = GetParent<ParentWithResources>().SomeInstrument;
            _parentsString = GetParent<ParentWithResources>().SomeString;

            // Check values
            if (_parentsDut == null)
                throw new InvalidOperationException($"No DUT found, for test step {Name}");
            if (_parentsInstrument == null)
                throw new InvalidOperationException($"No Instrument found, for test step {Name}");
        }

        public override void Run()
        {
            try
            {
                GetParentResources();

                Log.Debug($"Child={Name}, " +
                          $"_parentsDut={_parentsDut.Name}, " +
                          $"_parentsInstrument={_parentsInstrument.Name}, " +
                          $"_parentsString={_parentsString}.");

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