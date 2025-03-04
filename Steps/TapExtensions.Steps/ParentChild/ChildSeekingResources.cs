// This example shows how a child can get a parent with of a certain type, and then
// recover some properties from that parent.

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using OpenTap;

namespace TapExtensions.Steps.ParentChild
{
    [Display("ChildSeekingResources",
        Groups: new[] { "TapExtensions", "Steps", "ParentChild" })]
    [AllowAsChildIn(typeof(ParentWithResources))]
    public class ChildSeekingResources : TestStep
    {
        #region Settings

        [XmlIgnore]
        [Browsable(true)]
        [Display("SomeDut", Group: "From Parent", Collapsed: true)]
        public Dut SomeDut => GetParent<ParentWithResources>().SomeDut;

        [XmlIgnore]
        [Browsable(true)]
        [Display("SomeInstrument", Group: "From Parent", Collapsed: true)]
        public Instrument SomeInstrument => GetParent<ParentWithResources>().SomeInstrument;

        [XmlIgnore]
        [Browsable(true)]
        [Display("SomeString", Group: "From Parent", Collapsed: true)]
        public string SomeString => GetParent<ParentWithResources>().SomeString;

        #endregion

        public override void Run()
        {
            try
            {
                Log.Debug($"Child={Name}, " +
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