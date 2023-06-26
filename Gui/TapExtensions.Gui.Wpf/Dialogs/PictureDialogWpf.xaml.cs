using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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
        internal EBorderStyle BorderStyle { get; set; } = EBorderStyle.None;

        private DateTime _startTime;
        private DispatcherTimer _timer;

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
            SetDialogWindowStyle();
            SetDialogWindowBorders();
            SetDialogWindowControls();
            SetDialogWindowProperties();

            // Start the countdown timer
            if (Timeout > 0)
                StartTimer();

            // Call the base class to show the dialog window
            return ShowDialog();
        }

        private void SetDialogWindowBorders()
        {
            switch (BorderStyle)
            {
                case EBorderStyle.None:
                    BorderWithStripes.Margin = new Thickness(0);
                    BorderWithStripes.BorderThickness = new Thickness(0);
                    BorderWithStripes.Padding = new Thickness(0);
                    break;

                case EBorderStyle.Green:
                    // Colors.DarkGreen;
                    Stripe1.Color = Color.FromRgb(46, 204, 113);
                    Stripe2.Color = Color.FromRgb(46, 204, 113);
                    break;

                case EBorderStyle.Yellow:
                    Stripe1.Color = Color.FromRgb(241, 196, 15);
                    Stripe2.Color = Color.FromRgb(241, 196, 15);
                    break;

                case EBorderStyle.Orange:
                    Stripe1.Color = Color.FromRgb(230, 126, 34);
                    Stripe2.Color = Color.FromRgb(230, 126, 34);
                    break;

                case EBorderStyle.Red:
                    Stripe1.Color = Color.FromRgb(231, 76, 60);
                    Stripe2.Color = Color.FromRgb(231, 76, 60);
                    break;

                case EBorderStyle.Blue:
                    Stripe1.Color = Color.FromRgb(76, 142, 255);
                    Stripe2.Color = Color.FromRgb(76, 142, 255);
                    break;

                case EBorderStyle.Black:
                    Stripe1.Color = Colors.Black;
                    Stripe2.Color = Colors.Black;
                    break;

                case EBorderStyle.Gray:
                    Stripe1.Color = Colors.Gray;
                    Stripe2.Color = Colors.Gray;
                    break;

                case EBorderStyle.White:
                    Stripe1.Color = Colors.White;
                    Stripe2.Color = Colors.White;
                    break;
                default:
                    throw new ArgumentException(
                        $"Case not found for {nameof(BorderStyle)}={BorderStyle}");
            }
        }

        private void SetDialogWindowControls()
        {
            // Shot title
            if (!string.IsNullOrWhiteSpace(WindowTitle))
            {
                TitleTextBlock.Text = WindowTitle;
                TitleBar.Visibility = Visibility.Visible;
            }
            else
            {
                TitleBar.Visibility = Visibility.Collapsed;
            }

            // Show message
            if (!string.IsNullOrWhiteSpace(WindowMessage))
            {
                MessageTextBlock.Text = WindowMessage;
                MessageTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                MessageTextBlock.Visibility = Visibility.Collapsed;
            }

            // Show picture
            if (!string.IsNullOrWhiteSpace(WindowPicture))
            {
                Image.Source = new BitmapImage(new Uri(WindowPicture));
                Image.Visibility = Visibility.Visible;
            }
            else
            {
                Image.Visibility = Visibility.Collapsed;
            }

            // Change buttons
            switch (Buttons)
            {
                case EInputButtons.StartCancel:
                    LeftButton.Content = "Start";
                    RightButton.Content = "Cancel";
                    break;

                case EInputButtons.OkCancel:
                    LeftButton.Content = "OK";
                    RightButton.Content = "Cancel";
                    break;

                case EInputButtons.YesNo:
                    LeftButton.Content = "Yes";
                    RightButton.Content = "No";
                    break;

                default:
                    throw new ArgumentException(
                        $"Case not found for {nameof(Buttons)}={Buttons}");
            }
        }

        private void SetDialogWindowProperties()
        {
            // Change fontSize
            if (WindowFontSize > 0)
                DialogWindow.FontSize = WindowFontSize;

            // Change window width
            if (WindowMaxWidth > 0)
                DialogWindow.MaxWidth = WindowMaxWidth;

            // Change window height
            if (WindowMaxHeight > 0)
                DialogWindow.MaxHeight = WindowMaxHeight;

            // Change resize mode
            DialogWindow.ResizeMode = IsWindowResizable ? ResizeMode.CanResize : ResizeMode.NoResize;
        }

        private void CloseWindow()
        {
            if (_timer != null && _timer.IsEnabled)
                _timer.Stop();
            Close();
        }

        private void OnDragMoveWindow(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void OnOkButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            CloseWindow();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
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

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            // Resize window to default, on mouse double-click
            if (IsWindowResizable)
                SizeToContent = SizeToContent.WidthAndHeight;
        }

        private void StartTimer()
        {
            _startTime = DateTime.UtcNow;
            _timer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, 100) // Set interval to 100 milliseconds
            };
            _timer.Tick += OnTimedEvent;
            _timer.Start();
        }

        private void OnTimedEvent(object sender, EventArgs e)
        {
            var finishTime = _startTime + TimeSpan.FromSeconds(Timeout);
            var remainingDuration = finishTime - DateTime.UtcNow;

            if (remainingDuration <= TimeSpan.Zero)
            {
                DialogResult = false;
                CloseWindow();
            }
        }

        private void SetDialogWindowStyle()
        {
            // Change style
            DialogWindow.Style = GetApplicationStyle<Window>();

            try
            {
                // OuterBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 142, 255));
                // TitleTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(76, 142, 255));

                var b1 = GetApplicationResource<SolidColorBrush>("Tap.Highlight");
                OuterBorder.BorderBrush = b1;
                TitleTextBlock.Foreground = b1;

                // TitleBar.Background = new SolidColorBrush(Color.FromRgb(40, 40, 46));
                // TitleBar.Background = new SolidColorBrush(Color.FromRgb(220, 223, 236));
                // var a2 = GetApplicationResource<ImageBrush>("Window.TitleBar.Static.Background");

                var a1 = GetApplicationResource<LinearGradientBrush>("Tap.Header");
                TitleBar.Background = a1;
            }
            catch
            {
                // ignored
            }
        }

        private static T GetApplicationResource<T>(string resourceKey)
        {
            return (T)Application.Current.FindResource(resourceKey);
        }

        private static Style GetApplicationStyle<T>()
        {
            var tempStyle = Application.Current.Resources.Contains(typeof(T))
                ? new Style(typeof(T), (Style)Application.Current.Resources[typeof(T)])
                : new Style(typeof(T));
            return tempStyle;
        }
    }
}