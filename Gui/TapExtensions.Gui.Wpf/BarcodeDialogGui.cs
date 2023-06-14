﻿using System.Threading;
using System.Windows;
using TapExtensions.Interfaces.Gui;

namespace TapExtensions.Gui.Wpf
{
    public class BarcodeDialogGui : IGui
    {
        public string Message { get; set; } = "";
        public string Picture { get; set; } = "";
        public int Timeout { get; set; } = 0;
        public bool IsSerialNumberVisible { get; set; } = false;
        public bool IsProductCodeVisible { get; set; } = false;
        public EInputButtons Buttons { get; set; } = EInputButtons.StartCancel;
        public double FontSize { get; set; } = 0;
        public double MaxWidth { get; set; } = 0;
        public double MaxHeight { get; set; } = 0;
        public bool IsResizable { get; set; } = false;
        public EBorderStyle BorderStyle { get; set; } = EBorderStyle.None;

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
                    _wpfApp = new Application();
                    _wpfApp.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    _wpfApp.Run();
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();

                while (_wpfApp == null)
                    Thread.Sleep(150);

                _wpfApp.Dispatcher.Invoke(() =>
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
            var barcodeDialogWpf = new BarcodeDialogWpf(windowOwner)
            {
                WindowMessage = Message,
                WindowPicture = Picture,
                Timeout = Timeout,
                IsSerialNumberVisible = IsSerialNumberVisible,
                IsProductCodeVisible = IsProductCodeVisible,
                Buttons = Buttons,
                WindowFontSize = FontSize,
                WindowMaxWidth = MaxWidth,
                WindowMaxHeight = MaxHeight,
                IsWindowResizable = IsResizable,
                BorderStyle = BorderStyle
            };
            return barcodeDialogWpf.ShowWindow() == true;
        }
    }
}