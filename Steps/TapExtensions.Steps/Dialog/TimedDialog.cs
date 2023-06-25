﻿using System;
using OpenTap;
using TapExtensions.Gui.Wpf.Dialogs;
using TapExtensions.Interfaces.Gui;

namespace TapExtensions.Steps.Dialog
{
    [Display("TimedDialog", Groups: new[] { "TapExtensions", "Steps", "Dialog" })]
    public class TimedDialog : TestStep
    {
        #region Settings

        [Display("Message", Order: 1, Description: "The message shown to the user.")]
        [Layout(LayoutMode.Normal, 2, 6)]
        public string Message { get; set; }

        [Display("Timeout", Order: 2)]
        [Unit("s")]
        public Enabled<int> Timeout { get; set; }

        #endregion

        public TimedDialog()
        {
            // Default values
            Message = "Message";
            Timeout = new Enabled<int> { IsEnabled = false, Value = 60 };
        }

        public override void Run()
        {
            try
            {
                // Show dialog window
                IGui gui = new PictureDialogGui
                {
                    Message = Message,
                    Timeout = Timeout.IsEnabled ? Timeout.Value : 0
                };
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