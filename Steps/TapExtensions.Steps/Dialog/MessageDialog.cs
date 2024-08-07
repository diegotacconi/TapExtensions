﻿using System;
using OpenTap;
using TapExtensions.Gui.Wpf.Dialogs;
using TapExtensions.Interfaces.Gui;

namespace TapExtensions.Steps.Dialog
{
    [Display("MessageDialog", Groups: new[] { "TapExtensions", "Steps", "Dialog" })]
    public class MessageDialog : TestStep
    {
        #region Settings

        [Display("Title", Order: 1, Description: "The title of the dialog window.")]
        public string Title { get; set; }

        [Display("Message", Order: 2, Description: "The message shown to the user.")]
        [Layout(LayoutMode.Normal, 2, 6)]
        public string Message { get; set; }

        #endregion

        public MessageDialog()
        {
            // Default values
            Title = "Title";
            Message = "Message";
        }

        public override void Run()
        {
            try
            {
                IGui gui = new PictureDialogGui { Title = Title, Message = Message };
                var okayButton = gui.ShowDialog();

                // Check response from the user
                if (okayButton)
                    Log.Debug("User approved the dialog window");
                else
                    Log.Warning("User canceled the dialog window");

                UpgradeVerdict(okayButton ? Verdict.Pass : Verdict.Fail);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }
    }
}