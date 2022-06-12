using System;
using System.IO;
using OpenTap;
using TapExtensions.Gui.Wpf;
using TapExtensions.Interfaces.Gui;

namespace TapExtensions.Steps.Dialog
{
    [Display("PictureDialog", Groups: new[] { "TapExtensions", "Steps", "Dialog" })]
    public class PictureDialog : TestStep
    {
        #region Settings

        [Display("Title", Order: 1, Description: "The title of the dialog window.")]
        public string Title { get; set; }

        [Display("Message", Order: 2, Description: "The message shown to the user.")]
        [Layout(LayoutMode.Normal, 2, 6)]
        public string Message { get; set; }

        [Display("Picture", Order: 3, Description: "Path to the picture file such as 'C:\\image.jpg'.")]
        [FilePath]
        public string Picture
        {
            get => _fullPath;
            set => _fullPath = !string.IsNullOrWhiteSpace(value) ? Path.GetFullPath(value) : "";
        }

        private string _fullPath;

        [Display("Font Size", Order: 10, Group: "Dialog Window Properties", Collapsed: true,
            Description: "The size of the text.")]
        public Enabled<double> FontSize { get; set; }

        [Display("Max Width", Order: 11, Group: "Dialog Window Properties", Collapsed: true,
            Description: "The maximum width of the dialog window, in pixels.")]
        public Enabled<double> MaxWidth { get; set; }

        [Display("Max Height", Order: 12, Group: "Dialog Window Properties", Collapsed: true,
            Description: "The maximum height of the dialog window, in pixels.")]
        public Enabled<double> MaxHeight { get; set; }

        [Display("Is Resizable", Order: 13, Group: "Dialog Window Properties", Collapsed: true,
            Description: "Specifies whether the dialog window can be resized.")]
        public bool IsResizable { get; set; }

        [Display("Border Style", Order: 14, Group: "Dialog Window Properties", Collapsed: true)]
        public EBorderStyle BorderStyle { get; set; }

        #endregion

        public PictureDialog()
        {
            // Default values
            Title = "Title";
            Message = "Message";
            Picture = @"C:\Windows\Web\Screen\img103.png";
            FontSize = new Enabled<double> { IsEnabled = false, Value = 14 };
            MaxWidth = new Enabled<double> { IsEnabled = false, Value = 550 };
            MaxHeight = new Enabled<double> { IsEnabled = false, Value = 500 };
            IsResizable = true;
            BorderStyle = EBorderStyle.None;
        }

        public override void Run()
        {
            try
            {
                // Check if picture file exists
                if (!string.IsNullOrWhiteSpace(Picture) && !File.Exists(Picture))
                    throw new FileNotFoundException($"Cannot find picture file {Picture}");

                // Show dialog window
                IGui gui = new PictureDialogGui
                {
                    Title = Title,
                    Message = Message,
                    Picture = Picture,
                    FontSize = FontSize.IsEnabled ? FontSize.Value : 0,
                    MaxWidth = MaxWidth.IsEnabled ? MaxWidth.Value : 0,
                    MaxHeight = MaxHeight.IsEnabled ? MaxHeight.Value : 0,
                    IsResizable = IsResizable,
                    BorderStyle = BorderStyle
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