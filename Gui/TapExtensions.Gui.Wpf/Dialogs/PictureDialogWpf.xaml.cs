using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TapExtensions.Interfaces.Gui;

namespace TapExtensions.Gui.Wpf.Dialogs
{
    // ReSharper disable once RedundantExtendsListEntry
    internal partial class PictureDialogWpf : Window
    {
        internal string WindowTitle { get; set; } = "";
        internal string WindowMessage { get; set; } = "";
        internal string WindowPicture { get; set; } = "";
        internal int Timeout { get; set; } = 0;
        internal EInputButtons Buttons { get; set; } = EInputButtons.OkCancel;
        internal double WindowFontSize { get; set; } = 0;
        internal double WindowMaxWidth { get; set; } = 0;
        internal double WindowMaxHeight { get; set; } = 0;
        internal bool IsWindowResizable { get; set; } = false;

        private DateTime _StartTime;
        private DispatcherTimer _Timer;

        internal PictureDialogWpf(Window windowOwner = null)
        {
            InitializeComponent();

            // Center the window
            if (windowOwner != null)
            {
                Owner = windowOwner;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                Topmost = true;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        internal bool? ShowWindow()
        {
            if (!string.IsNullOrWhiteSpace(WindowTitle))
                DialogWindow.Title = WindowTitle;

            if (!string.IsNullOrWhiteSpace(WindowMessage))
            {
                TextBlockMessage.Text = WindowMessage;
                TextBlockMessage.Visibility = Visibility.Visible;
            }
            else
            {
                TextBlockMessage.Visibility = Visibility.Collapsed;
            }

            if (!string.IsNullOrWhiteSpace(WindowPicture))
            {
                Image.Source = new BitmapImage(new Uri(WindowPicture));
                Image.Visibility = Visibility.Visible;
            }
            else
            {
                Image.Visibility = Visibility.Collapsed;
            }

            switch (Buttons)
            {
                case EInputButtons.StartCancel:
                    ButtonOk.Content = "Start";
                    ButtonCancel.Content = "Cancel";
                    break;

                case EInputButtons.OkCancel:
                    ButtonOk.Content = "OK";
                    ButtonCancel.Content = "Cancel";
                    break;

                case EInputButtons.YesNo:
                    ButtonOk.Content = "Yes";
                    ButtonCancel.Content = "No";
                    break;

                default:
                    throw new ArgumentException(
                        $"Case not found for {nameof(Buttons)}={Buttons}");
            }

            if (WindowFontSize > 0)
                DialogWindow.FontSize = WindowFontSize;

            if (WindowMaxWidth > 0)
                DialogWindow.MaxWidth = WindowMaxWidth;

            if (WindowMaxHeight > 0)
                DialogWindow.MaxHeight = WindowMaxHeight;

            DialogWindow.ResizeMode = IsWindowResizable ? ResizeMode.CanResize : ResizeMode.NoResize;

            // Start the countdown timer
            if (Timeout > 0)
                StartTimer();

            // Call the base class to show the dialog window
            return ShowDialog();
        }

        private void CloseWindow()
        {
            if (_Timer != null && _Timer.IsEnabled)
                _Timer.Stop();
            Close();
        }

        private void OnButtonOkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            CloseWindow();
        }

        private void OnButtonCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            CloseWindow();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            // Close window if escape key is pressed
            if (keyEventArgs.Key == Key.Escape)
            {
                DialogResult = false;
                CloseWindow();
            }
        }

        // Resize window to default, on mouse double-click
        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (IsWindowResizable)
                SizeToContent = SizeToContent.WidthAndHeight;
        }

        private void StartTimer()
        {
            _StartTime = DateTime.UtcNow;
            _Timer = new DispatcherTimer();
            _Timer.Interval = new TimeSpan(0, 0, 0, 0, 100); // Set interval to 100 milliseconds
            _Timer.Tick += OnTimedEvent;
            _Timer.Start();
        }

        private void OnTimedEvent(object sender, EventArgs e)
        {
            var finishTime = _StartTime + TimeSpan.FromSeconds(Timeout);
            var remainingDuration = finishTime - DateTime.UtcNow;

            if (remainingDuration <= TimeSpan.Zero)
            {
                DialogResult = false;
                CloseWindow();
            }
        }
    }
}