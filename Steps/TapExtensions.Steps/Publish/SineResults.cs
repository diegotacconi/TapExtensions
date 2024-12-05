using System;
using System.Collections.Generic;
using OpenTap;

namespace TapExtensions.Steps.Publish
{
    [Display("SineResults",
        Groups: new[] { "TapExtensions", "Steps", "Publish" },
        Description: "Generates a sine wave with settable amplitude and point count")]
    public class SineResults : TestStep
    {
        #region Settings

        [Display("Result Name", Order: 1, Group: "Parameters",
            Description: "Name of this result.")]
        public string ResultName { get; set; }

        [Display("Point Count", Order: 2, Group: "Parameters",
            Description: "Number of points to generate.")]
        public int PointCount { get; set; }

        [Display("Amplitude", Order: 3, Group: "Parameters",
            Description: "Distance from the Mean (average) value to the peak of the sine wave, " +
                         "representing the maximum deviation from the Mean.")]
        public double Amplitude { get; set; }

        [Display("Mean", Order: 4, Group: "Parameters",
            Description: "Shift of the entire sine wave from the midline, changing the average value of the wave.")]
        public double Mean { get; set; }

        [Display("Noise StdDev", Order: 5, Group: "Parameters",
            Description: "Standard deviation of noise.")]
        public Enabled<double> NoiseStdDev { get; set; }

        [Display("Lower Limit", Order: 6, Group: "Limits")]
        public double LowerLimit { get; set; }

        [Display("Upper Limit", Order: 7, Group: "Limits")]
        public double UpperLimit { get; set; }

        #endregion

        public SineResults()
        {
            // Default values
            ResultName = "SineResults";
            PointCount = 50;
            Amplitude = 20;
            Mean = 20;
            NoiseStdDev = new Enabled<double> { IsEnabled = false, Value = 3 };
            LowerLimit = 0;
            UpperLimit = 40;

            // Validation rules
            Rules.Add(() => LowerLimit <= UpperLimit,
                "Lower limit cannot be greater than upper limit", nameof(LowerLimit));
            Rules.Add(() => LowerLimit <= UpperLimit,
                "Lower limit cannot be greater than upper limit", nameof(UpperLimit));
        }

        public override void Run()
        {
            var traceBar = new TraceBar
            {
                LowerLimit = LowerLimit,
                UpperLimit = UpperLimit,
                BarLength = 50
            };

            var values = new double[PointCount];
            var indices = new int[PointCount];
            for (var i = 0; i < PointCount; i++)
            {
                var radians = i * (2 * Math.PI / PointCount);
                var pureValue = Mean + Amplitude * Math.Sin(radians);
                var randomAddon = NoiseStdDev.IsEnabled ? RandomValue.CalcValue(0, NoiseStdDev.Value) : 0;
                var value = pureValue + randomAddon;

                Log.Debug(traceBar.GetBar(value));
                CheckLimits(value);

                values[i] = value;
                indices[i] = i;
            }

            Results.PublishTable(ResultName, new List<string> { "Index", "Value" }, indices, values);
        }

        protected void CheckLimits(double value)
        {
            if (value < LowerLimit || value > UpperLimit)
                UpgradeVerdict(Verdict.Fail);
            else
                UpgradeVerdict(Verdict.Pass);
        }
    }

    internal class RandomValue
    {
        private static readonly Random RandomSeed = new Random();

        /// <summary> Calculates a random value within a standard deviation. </summary>
        public static double CalcValue(double mean, double stdDev)
        {
            var u1 = RandomSeed.NextDouble();
            var u2 = RandomSeed.NextDouble();
            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            var randNormal = mean + stdDev * randStdNormal;
            return randNormal;
        }
    }
}