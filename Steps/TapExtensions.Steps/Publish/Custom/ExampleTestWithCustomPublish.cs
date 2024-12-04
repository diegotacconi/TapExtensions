using System;
using OpenTap;

namespace TapExtensions.Steps.Publish.Custom
{
    [Display("ExampleTestWithCustomPublish",
        Groups: new[] { "TapExtensions", "Steps", "Publish", "Custom" })]
    public class ExampleTestWithCustomPublish : TestStepBaseWithCustomPublish
    {
        #region Settings

        [Display("Time Delay", Description: "Time delay between publishing result statements")]
        [Unit("s")]
        public double TimeDelay { get; set; }

        #endregion

        public ExampleTestWithCustomPublish()
        {
            // Default values
            TimeDelay = 0.1;

            // Validation rules
            Rules.Add(() => TimeDelay >= 0,
                "Time delay must be greater than or equal to zero", nameof(TimeDelay));
        }

        public override void Run()
        {
            var random = new Random(Guid.NewGuid().GetHashCode());

            Sleep(TimeDelay);
            Publish($"{Name}_bool", true, true, true, "bool");

            Sleep(TimeDelay);
            Publish($"{Name}_int", random.Next(0, 100), 0, 100, "int");

            Sleep(TimeDelay);
            Publish($"{Name}_double", Math.Round(random.NextDouble() * 100, 6), 0, 100, "double");

            Sleep(TimeDelay);
            Publish<decimal>($"{Name}_decimal", random.Next(-10000, 10000), -10000, 10000, "decimal");

            Sleep(TimeDelay);
            Publish($"{Name}_bytes", random.Next(0, 65535), 0x0, 0xFFFF, EBase.Base16, "bytes");

            Sleep(TimeDelay);
            Publish($"{Name}_string", "ABC", "ABC", "ABC", "str");
        }

        private void Sleep(double timeDelay)
        {
            TapThread.Sleep(TimeSpan.FromSeconds(timeDelay));
        }
    }
}