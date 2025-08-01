using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using OpenTap;

namespace TapExtensions.Steps.ParentChild
{
    [Display("TemperatureAverage",
        Groups: new[] { "TapExtensions", "Steps", "ParentChild" })]
    [AllowAsChildIn(typeof(Temperature))]
    public class TemperatureAverage : TestStep
    {
        [XmlIgnore]
        [Browsable(true)]
        [Display("Measurements", Group: "From Parent", Collapsed: true)]
        public List<double> Measurements => GetParent<Temperature>().Measurements;

        public override void Run()
        {
            try
            {
                foreach (var m in Measurements)
                    Log.Debug($"{m}");

                var average = Measurements.Average();
                Log.Debug($"average = {average}");

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