using System;
using System.Windows;

namespace TapExtensions.Gui.Wpf
{
    public class PictureDialogGui
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Picture { get; set; } = "";
        public double FontSize { get; set; } = 0;
        public double MaxWidth { get; set; } = 0;
        public double MaxHeight { get; set; } = 0;
        public bool IsResizable { get; set; } = false;

        public bool ShowDialog()
        {
            // Check if we are running in a GUI or a Console process
            if (Application.Current == null)
                throw new InvalidOperationException(
                    $"The {nameof(PictureDialogGui)} does not work in a console process");

            // Initialize variables
            var result = false;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var windowOwner = Application.Current.MainWindow;
                var pictureDialogWpf = new PictureDialogWpf(windowOwner)
                {
                    WindowTitle = Title,
                    WindowMessage = Message,
                    WindowPicture = Picture,
                    WindowFontSize = FontSize,
                    WindowMaxWidth = MaxWidth,
                    WindowMaxHeight = MaxHeight,
                    IsWindowResizable = IsResizable
                };

                result = pictureDialogWpf.ShowWindow() == true;
            });

            return result;
        }
    }
}