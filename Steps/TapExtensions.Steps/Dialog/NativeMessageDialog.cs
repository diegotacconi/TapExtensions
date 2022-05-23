using System;
using System.ComponentModel;
using OpenTap;

namespace TapExtensions.Steps.Dialog
{
    [Display("NativeMessageDialog", Groups: new[] { "TapExtensions", "Steps", "Dialog" })]
    public class NativeMessageDialog : TestStep
    {
        #region Settings

        [Display("Title", Order: 1, Description: "The title of the dialog window.")]
        public string Title { get; set; }

        [Display("Message", Order: 2, Description: "The message shown to the user.")]
        [Layout(LayoutMode.Normal, 2, 6)]
        public string Message { get; set; }

        #endregion

        public NativeMessageDialog()
        {
            // Default values
            Title = "Title";
            Message = "Message";
        }

        public override void Run()
        {
            try
            {
                // Show dialog window
                var dialog = new DialogWindow(Title, Message);
                UserInput.Request(dialog, true);

                // Check and log the selected button
                var logMsg = $"User selected the '{dialog.Response}' button";
                var result = dialog.Response == EDialogButton.Ok;
                if (result)
                    Log.Debug(logMsg);
                else
                    Log.Warning(logMsg);

                // Publish(Name, result, true, true, "bool");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                // Publish(Name, false, true, true, "bool");
            }
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

            [Browsable(true)] // Show it even though it is read-only
            [Layout(LayoutMode.FullRow, 2)] // Set the layout to fill the entire row
            public string Message { get; }

            [Layout(LayoutMode.FloatBottom | LayoutMode.FullRow)] // Show the button selection at the bottom of the window
            [Submit] // When a button is clicked the result is 'submitted', so the dialog is closed
            public EDialogButton Response { get; set; } = EDialogButton.Ok;
        }

        internal enum EDialogButton
        {
            // The number assigned, determines the order in which the buttons are shown in the dialog
            Ok = 1,
            Cancel = 2
        }
    }
}