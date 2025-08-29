using System;
using OpenTap;

namespace TapExtensions.Steps.RuntimeVariables
{
    [Display("SetRuntimeVariables",
        Groups: new[] { "TapExtensions", "Steps", "RuntimeVariables" })]
    public class SetRuntimeVariables : TestStep
    {
        [Display("VariableName", Order: 1)] public string VariableName { get; set; } = "MyVariable";

        [Display("VariableValue", Order: 2)] public double VariableValue { get; set; } = 123;

        public override void Run()
        {
            try
            {
                try
                {
                    RuntimeVariables.Set(VariableName, VariableValue);
                    Log.Debug($"{Name}: Set({VariableName}, {VariableValue})");
                }
                catch (Exception)
                {
                    RuntimeVariables.Add(VariableName, VariableValue);
                    Log.Debug($"{Name}: Add({VariableName}, {VariableValue})");
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