using System;
using OpenTap;

namespace TapExtensions.Steps.RuntimeVariables
{
    [Display("GetRuntimeVariables",
        Groups: new[] { "TapExtensions", "Steps", "RuntimeVariables" })]
    public class GetRuntimeVariables : TestStep
    {
        [Display("VariableName", Order: 1)] public string VariableName { get; set; } = "MyVariable";

        [Display("DeleteVariable", Order: 2)] public bool DeleteVariable { get; set; } = true;

        public override void Run()
        {
            try
            {
                RuntimeVariables.Get(VariableName, out double value);
                Log.Debug($"{Name}: Get({VariableName}) returned {value}");

                if (DeleteVariable)
                {
                    RuntimeVariables.Delete(VariableName);
                    Log.Debug($"{Name}: Delete({VariableName})");
                }

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