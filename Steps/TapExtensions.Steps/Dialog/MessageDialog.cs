using System;
using OpenTap;
using TapExtensions.Gui.Wpf.DialogsWithBorders;
using TapExtensions.Interfaces.Gui;

namespace TapExtensions.Steps.Dialog
{
    [Display("MessageDialog", Groups: new[] { "TapExtensions", "Steps", "Dialog" })]
    public class MessageDialog : TestStep
    {
        #region Settings

        [Display("Message", Order: 1, Description: "The message shown to the user.")]
        [Layout(LayoutMode.Normal, 2, 6)]
        public string Message { get; set; }

        #endregion

        public MessageDialog()
        {
            // Default values
            Message = "Message";
        }

        public override void Run()
        {
            try
            {
                // Show dialog window
                IGui gui = new PictureDialogGui { Message = Message };
                var result = gui.ShowDialog();

                // Check response from the user
                if (result)
                    Log.Debug("User approved the dialog window");
                else
                    Log.Warning("User canceled the dialog window");

                // Publish(Name, result, true, true, "bool");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                // Publish(Name, false, true, true, "bool");
            }
        }
    }
}