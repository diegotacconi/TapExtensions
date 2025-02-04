using System;
using OpenTap;

namespace TapExtensions.Steps.FlowControl
{
    [Display("If Verdict",
        Groups: new[] { "TapExtensions", "Steps", "FlowControl" },
        Description: "Runs its child steps only when the verdict of another step has a specific value.")]
    [AllowAnyChild]
    public class IfVerdict : TestStep
    {
        #region Settings

        [Flags]
        public enum VerdictFlags
        {
            [Display("Not Set")] NotSet = 1,
            Pass = 2,
            Inconclusive = 4,
            Fail = 8,
            Aborted = 16,
            Error = 32
        }

        public enum IfStepAction
        {
            [Display("Run Children")] RunChildren,
            [Display("Abort Test Plan")] AbortTestPlan
        }

        [Display("If", Order: 1)] public Input<Verdict> InputVerdict { get; set; }

        [Display("Equals", Order: 2)] public VerdictFlags TargetVerdictFlags { get; set; }

        [Display("Then", Order: 3)] public IfStepAction Action { get; set; }

        #endregion

        public IfVerdict()
        {
            InputVerdict = new Input<Verdict>();
            Rules.Add(() => InputVerdict.Step != null,
                "Input property must be set.", nameof(InputVerdict));
        }

        private bool IsVerdictSelected(Verdict verdict)
        {
            return (verdict == Verdict.NotSet && TargetVerdictFlags.HasFlag(VerdictFlags.NotSet)) ||
                   (verdict == Verdict.Pass && TargetVerdictFlags.HasFlag(VerdictFlags.Pass)) ||
                   (verdict == Verdict.Inconclusive && TargetVerdictFlags.HasFlag(VerdictFlags.Inconclusive)) ||
                   (verdict == Verdict.Fail && TargetVerdictFlags.HasFlag(VerdictFlags.Fail)) ||
                   (verdict == Verdict.Aborted && TargetVerdictFlags.HasFlag(VerdictFlags.Aborted)) ||
                   (verdict == Verdict.Error && TargetVerdictFlags.HasFlag(VerdictFlags.Error));
        }

        public override void Run()
        {
            // Get the target step
            if (InputVerdict == null)
                throw new ArgumentException("Could not locate target test step");

            if (IsVerdictSelected(InputVerdict.Value))
            {
                switch (Action)
                {
                    case IfStepAction.RunChildren:
                        Log.Info("Condition is true, running childSteps");
                        RunChildSteps();
                        break;

                    case IfStepAction.AbortTestPlan:
                        Log.Info("Condition is true, aborting TestPlan run.");
                        PlanRun.MainThread.Abort();
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                Log.Info("Condition is false.");
            }
        }
    }
}