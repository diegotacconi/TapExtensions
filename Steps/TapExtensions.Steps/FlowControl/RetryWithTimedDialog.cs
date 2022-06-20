using System;
using System.ComponentModel;
using System.Xml.Serialization;
using OpenTap;

namespace TapExtensions.Steps.FlowControl
{
    [Display("RetryWithTimedDialog",
        Groups: new[] { "TapExtensions", "Steps", "FlowControl" },
        Description: "Run all child steps until all verdicts are passing or max number of attempts is reached.")]
    [AllowAnyChild]
    public class RetryWithTimedDialog : TestStep
    {
        #region Settings

        [Display("Max Count", Order: 1,
            Description: "Maximum number of iteration attempts for repeating child steps.")]
        public int MaxCount { get; set; }

        [XmlIgnore]
        [Browsable(false)]
        public int Iteration { get; set; }

        [Display("Message", Order: 3, Group: "Dialog",
            Description: "The message shown to the user.")]
        [Layout(LayoutMode.Normal, 2, 6)]
        public string Message { get; set; }

        [Display("Timeout", Order: 4, Group: "Dialog",
            Description: "When enabled, the dialog will close after an amount of time.")]
        [Unit("s")]
        public Enabled<double> Timeout { get; set; }

        #endregion

        public RetryWithTimedDialog()
        {
            // Default values
            MaxCount = 3;
            Message = "Would you like to retry this test?";
            Timeout = new Enabled<double> { IsEnabled = true, Value = 5 };

            // Validation rules
            Rules.Add(() => MaxCount >= 1,
                "Max Count must be greater than or equal to one", nameof(MaxCount));
            Rules.Add(() => (!Timeout.IsEnabled || Timeout.Value > 0),
                "When enabled, the timeout value must be greater than zero", nameof(Timeout));
        }

        public override void PrePlanRun()
        {
            ThrowOnValidationError(false);

            if (Timeout.IsEnabled && Timeout.Value <= 0)
                throw new InvalidOperationException("Timeout value must be greater than zero");
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
                    try
                    {
                        TimeSpan timeout = TimeSpan.MaxValue;
                        if (Timeout.IsEnabled && Timeout.Value > 0)
                            timeout = TimeSpan.FromSeconds(Timeout.Value);

                        UserInput.Request(dialog, timeout, true);
                        Log.Debug($"User selected the '{dialog.Response}' button");

                        // Exit loop if operator does not want to continue retrying
                        if (dialog.Response != EDialogButton.Retry)
                            break;
                    }
                    catch (TimeoutException)
                    {
                        // Exit loop if timeout occurred
                        Log.Debug("Dialog timed-out");
                        break;
                    }
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