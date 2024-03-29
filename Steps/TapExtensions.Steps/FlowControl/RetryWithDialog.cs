﻿using System;
using System.ComponentModel;
using System.Xml.Serialization;
using OpenTap;

namespace TapExtensions.Steps.FlowControl
{
    [Display("RetryWithDialog",
        Groups: new[] { "TapExtensions", "Steps", "FlowControl" },
        Description: "Run all child steps until all verdicts are passing or max number of attempts is reached.")]
    [AllowAnyChild]
    public class RetryWithDialog : TestStep
    {
        #region Settings

        [Display("Max Count", Order: 1,
            Description: "Maximum number of iteration attempts for repeating child steps.")]
        public int MaxCount { get; set; }

        [XmlIgnore]
        [Browsable(false)]
        public int Iteration { get; set; }

        [Display("Dialog message", Order: 3, Description: "The message shown to the user.")]
        [Layout(LayoutMode.Normal, 2, 6)]
        public string Message { get; set; }

        #endregion

        public RetryWithDialog()
        {
            // Default values
            MaxCount = 3;
            Message = "Would you like to retry this test?";

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
                {
                    var msg = $"Retrying attempt {Iteration} of {MaxCount} ...";
                    Log.Warning(msg);

                    // Ask the operator to continue retrying
                    var dialog = new DialogWindow(msg, Message);
                    UserInput.Request(dialog, true);
                    Log.Debug($"User selected the '{dialog.Response}' button");

                    // Exit loop if operator does not want to continue retrying
                    if (dialog.Response != EDialogButton.Retry)
                        break;
                }

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

        // Describes the dialog window
        internal class DialogWindow
        {
            public DialogWindow(string title, string message)
            {
                Name = title;
                Message = message;
            }

            public string Name { get; } // Title of dialog window

            [Layout(LayoutMode.FullRow, 2)] // Set the layout to fill the entire row
            [Browsable(true)] // Show it even though it is read-only
            public string Message { get; }

            [Layout(LayoutMode.FloatBottom | LayoutMode.FullRow)] // Show the button selection at the bottom of the window
            [Submit] // When a button is clicked the result is 'submitted', so the dialog is closed
            public EDialogButton Response { get; set; } = EDialogButton.Retry;
        }

        public enum EDialogButton
        {
            // The number assigned, determines the order in which the buttons are shown in the dialog
            Retry = 1,
            Cancel = 2
        }
    }
}