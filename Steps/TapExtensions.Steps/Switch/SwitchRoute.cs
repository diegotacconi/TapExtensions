using System;
using OpenTap;
using TapExtensions.Interfaces.Switch;

namespace TapExtensions.Steps.Switch
{
    [Display("SwitchRoute",
        Groups: new[] { "TapExtensions", "Steps", "Switch" })]
    public class SwitchRoute : TestStep
    {
        [Display("Switch", Order: 1)] public ISwitch Switch { get; set; }

        [Display("Route", Order: 2)] public string Route { get; set; }

        public override void Run()
        {
            try
            {
                Log.Debug($"Setting route '{Route}' on {Switch}");
                Switch.SetRoute(Route);
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