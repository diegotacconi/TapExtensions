using System.Threading;
using System.Windows;
using TapExtensions.Interfaces.Gui;

namespace TapExtensions.Gui.Wpf.Dialogs
{
    public class PictureDialogGui : IGui
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Picture { get; set; } = "";
        public int Timeout { get; set; } = 0;
        public EInputButtons Buttons { get; set; } = EInputButtons.OkCancel;
        public double FontSize { get; set; } = 0;
        public double MaxWidth { get; set; } = 0;
        public double MaxHeight { get; set; } = 0;
        public bool IsResizable { get; set; } = false;
        public string BorderColor { get; set; } = "";

        private Application _wpfApp;

        public bool ShowDialog()
        {
            var result = false;

            // Check if we are running in a GUI or a Console process
            if (Application.Current == null)
            {
                // When called from a Console process
                var thread = new Thread(() =>
                {
                    _wpfApp = new Application
                    {
                        ShutdownMode = ShutdownMode.OnExplicitShutdown
                    };
                    _wpfApp.Run();
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();

                while (_wpfApp == null)
                    Thread.Sleep(150);

                _wpfApp.Dispatcher.Invoke(() => { result = CallShowWindow(); });
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
                Timeout = Timeout,
                Buttons = Buttons,
                WindowFontSize = FontSize,
                WindowMaxWidth = MaxWidth,
                WindowMaxHeight = MaxHeight,
                IsWindowResizable = IsResizable,
                BorderColor = BorderColor
            };
            return pictureDialogWpf.ShowWindow() == true;
        }
    }
}