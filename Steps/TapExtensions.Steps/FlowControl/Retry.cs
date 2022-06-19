using System;
using System.ComponentModel;
using System.Xml.Serialization;
using OpenTap;

namespace TapExtensions.Steps.FlowControl
{
    [Display("Retry",
        Groups: new[] { "TapExtensions", "Steps", "FlowControl" },
        Description: "Run all child steps until all verdicts are passing or max number of attempts is reached.")]
    [AllowAnyChild]
    public class Retry : TestStep
    {
        #region Settings

        [Display("Max Count", Order: 1,
            Description: "Maximum number of iteration attempts for repeating child steps.")]
        public int MaxCount { get; set; }

        [XmlIgnore]
        [Browsable(false)]
        public int Iteration { get; set; }

        #endregion

        public Retry()
        {
            // Default values
            MaxCount = 3;

            // Validation rules
            Rules.Add(() => MaxCount >= 1,
                "Max Count must be greater than or equal to one", nameof(MaxCount));
        }

        public override void PrePlanRun()
        {
            // Block the test step from being run if there are any validation errors with the current values.
            ThrowOnValidationError(true);
        }

        public override void Run()
        {
            Iteration = 0;
            while (Iteration < MaxCount)
            {
                Iteration++;

                if (Iteration > 1)
                    Log.Warning($"Retrying attempt {Iteration} of {MaxCount} ...");

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