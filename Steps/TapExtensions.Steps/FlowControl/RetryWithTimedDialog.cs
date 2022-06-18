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

        [Display("Max number of attempts", Order: 1,
            Description: "Maximum number of iteration attempts for repeating child steps.")]
        public uint MaxNumberOfAttempts { get; set; }

        [XmlIgnore]
        [Browsable(false)]
        public int Iteration { get; set; }

        [Display("Dialog message", Order: 3, Description: "The message shown to the user.")]
        [Layout(LayoutMode.Normal, 2, 6)]
        public string Message { get; set; }

        [Display("Timeout", Order: 4,
            Description: "Enabling this will close the dialog window after an amount of time.")]
        [Unit("s")]
        public Enabled<double> Timeout { get; set; }

        #endregion

        public RetryWithTimedDialog()
        {
            MaxNumberOfAttempts = 3;
            Message = "Would you like to retry this test?";
            Timeout = new Enabled<double> { IsEnabled = true, Value = 5 };
        }

        public override void Run()
        {
            Iteration = 0;
            while (Iteration < MaxNumberOfAttempts)
            {
                Iteration++;

                if (Iteration > 1)
                {
                    var msg = $"Retrying attempt {Iteration} of {MaxNumberOfAttempts} ...";
                    Log.Warning(msg);

                    // Ask the operator to continue retrying
                    var dialog = new DialogWindow(msg, Message);
                    try
                    {
                        var timeout = TimeSpan.FromSeconds(Timeout.Value);
                        if (timeout == TimeSpan.Zero)
                            timeout = TimeSpan.FromSeconds(0.001);
                        if (Timeout.IsEnabled == false)
                            timeout = TimeSpan.MaxValue;
                        UserInput.Request(dialog, timeout, true);
                        Log.Debug($"User selected the '{dialog.Response}' button");

                        // Exit loop if operator does not want to continue retrying
                        if (dialog.Response != EDialogButton.Retry)
                            break;
                    }
                    catch (TimeoutException)
                    {
                        Log.Debug("Dialog timed-out");
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