using System.ComponentModel;
using System.Xml.Serialization;
using OpenTap;

namespace TapExtensions.Steps.FlowControl
{
    [Display("Retry",
        Groups: new[] { "TapExtensions", "Steps", "FlowControl" },
        Description: "Run all child steps until all verdicts are passing or max iteration is reached.")]
    [AllowAnyChild]
    public class Retry : TestStep
    {
        #region Settings

        [Display("Max repeat count", "Maximum number of iteration attempts for repeating child steps")]
        public uint MaxIteration { get; set; } = 3;

        [XmlIgnore]
        [Browsable(false)]
        public int Iteration { get; set; }

        #endregion

        public override void Run()
        {
            Iteration = 0;
            while (Iteration < MaxIteration)
            {
                Iteration++;

                if (Iteration > 1)
                    Log.Warning($"Retrying attempt {Iteration} of {MaxIteration} ...");

                ResetResultOfChildSteps();
                RunChildSteps();
                Verdict = Verdict.NotSet;
                UpgradeVerdictWithChildVerdicts();

                // Exit loop if retry attempt is successful
                if (Verdict == Verdict.Pass)
                    break;
            }
        }

        private void ResetResultOfChildSteps()
        {
            var enabledChildSteps = RecursivelyGetChildSteps(TestStepSearch.EnabledOnly);
            foreach (var step in enabledChildSteps)
                step.Verdict = Verdict.NotSet;
        }

        private void UpgradeVerdictWithChildVerdicts()
        {
            var enabledChildSteps = RecursivelyGetChildSteps(TestStepSearch.EnabledOnly);
            foreach (var step in enabledChildSteps)
                UpgradeVerdict(step.Verdict);
        }
    }
}