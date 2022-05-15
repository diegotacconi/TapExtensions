using System;
using System.Threading;
using System.Windows;

namespace TapExtensions.Gui.Wpf
{
    public class PictureDialogGui
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Picture { get; set; } = "";
        public InputButtons Buttons { get; set; } = InputButtons.OkCancel;
        public double FontSize { get; set; } = 0;
        public double MaxWidth { get; set; } = 0;
        public double MaxHeight { get; set; } = 0;
        public bool IsResizable { get; set; } = false;

        private Application WpfApp;

        public bool ShowDialog()
        {
            var result = false;

            // Check if we are running in a GUI or a Console process
            if (Application.Current == null)
            {
                // When called from a Console process
                var thread = new Thread(() =>
                {
                    WpfApp = new Application();
                    WpfApp.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    WpfApp.Run();
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();

                while (WpfApp == null)
                    Thread.Sleep(150);

                WpfApp.Dispatcher.Invoke(() =>
                {
                    result = CallShowWindow();
                });
            }
            else
            {
                // When called from a WPF GUI process
                Application.Current.Dispatcher.Invoke(() =>
                {
                    result = CallShowWindow(Application.Current.MainWindow);
                });
            }

            return result;
        }

        private bool CallShowWindow(Window windowOwner = null)
        {
            var pictureDialogWpf = new PictureDialogWpf(windowOwner)
            {
                WindowTitle = Title,
                WindowMessage = Message,
                WindowPicture = Picture,
                Buttons = Buttons,
                WindowFontSize = FontSize,
                WindowMaxWidth = MaxWidth,
                WindowMaxHeight = MaxHeight,
                IsWindowResizable = IsResizable
            };
            return pictureDialogWpf.ShowWindow() == true;
        }
    }
}