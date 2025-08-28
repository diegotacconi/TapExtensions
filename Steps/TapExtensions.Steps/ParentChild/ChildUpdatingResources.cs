using System;
using System.ComponentModel;
using System.Xml.Serialization;
using OpenTap;

namespace TapExtensions.Steps.ParentChild
{
    [Display("ChildUpdatingResources",
        Groups: new[] { "TapExtensions", "Steps", "ParentChild" })]
    [AllowAsChildIn(typeof(ParentWithResources))]
    public class ChildUpdatingResources : TestStep
    {
        [XmlIgnore]
        [Browsable(false)]
        public string SomeString
        {
            get => GetParent<ParentWithResources>().SomeString;
            set => GetParent<ParentWithResources>().SomeString = value;
        }

        [Display("AddToSomeString", Order: 3)] public string AddToSomeString { get; set; } = "DEF";

        public override void Run()
        {
            try
            {
                Log.Debug($"Child={Name}(before), String={SomeString}.");

                SomeString += AddToSomeString;

                Log.Debug($"Child={Name}(before), String={SomeString}.");

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