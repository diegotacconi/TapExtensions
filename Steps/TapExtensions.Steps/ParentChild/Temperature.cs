using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using OpenTap;

namespace TapExtensions.Steps.ParentChild
{
    [Display("Temperature",
        Groups: new[] { "TapExtensions", "Steps", "ParentChild" })]
    [AllowChildrenOfType(typeof(TemperatureAverage))]
    public class Temperature : TestStep
    {
        [XmlIgnore]
        [Browsable(false)]
        public List<double> Measurements { get; } = new List<double>();

        public override void Run()
        {
            try
            {
                Measurements.Clear();

                var random = new Random();
                for (var i = 1; i <= 5; i++)
                {
                    var value = Math.Round(random.NextDouble() * 5, 3);
                    Measurements.Add(value);
                }

                foreach (var m in Measurements)
                    Log.Debug($"{m}");

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