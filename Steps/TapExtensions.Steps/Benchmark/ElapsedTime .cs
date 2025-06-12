using System;
using OpenTap;

namespace TapExtensions.Steps.Benchmark
{
    [Display("ElapsedTime",
        Groups: new[] { "TapExtensions", "Steps", "Benchmark" })]
    public class ElapsedTime : TestStep
    {
        public override void Run()
        {
            var startTime = PlanRun.StartTime;
            Log.Debug($"startTime = {startTime}");

            var elapsedTime = DateTime.Now - startTime;
            Log.Debug($"elapsedTime = {elapsedTime}");
        }
    }
}