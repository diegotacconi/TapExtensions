using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TapExtensions.Interfaces.Gui;

namespace TapExtensions.Gui.Wpf
{
    // ReSharper disable once RedundantExtendsListEntry
    internal partial class PictureDialogWpf : Window
    {
        internal string WindowMessage { get; set; } = "";
        internal string WindowPicture { get; set; } = "";
        internal int Timeout { get; set; } = 0;
        internal EInputButtons Buttons { get; set; } = EInputButtons.OkCancel;
        internal double WindowFontSize { get; set; } = 0;
        internal double WindowMaxWidth { get; set; } = 0;
        internal double WindowMaxHeight { get; set; } = 0;
        internal bool IsWindowResizable { get; set; } = true;
        internal EBorderStyle BorderStyle { get; set; } = EBorderStyle.None;

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
            SetDialogWindowStyle();
            SetDialogWindowControls();
            SetDialogWindowProperties();

            // Start the countdown timer
            if (Timeout > 0)
                StartTimer();

            // Call the base class to show the dialog window
            return ShowDialog();
        }

        private void SetDialogWindowStyle()
        {
            // Change style
            DialogWindow.Style = GetApplicationStyle<Window>();
            MainWindowBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 142, 255));
            switch (BorderStyle)
            {
                case EBorderStyle.None:
                    BorderWithStripes.BorderThickness = new Thickness(0);
                    BorderWithStripes.Margin = new Thickness(0);
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
            // Show message
            if (!string.IsNullOrWhiteSpace(WindowMessage))
            {
                TextBlockMessage.Text = WindowMessage;
                TextBlockMessage.Visibility = Visibility.Visible;
            }
            else
            {
                TextBlockMessage.Visibility = Visibility.Collapsed;
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
            if (_Timer != null && _Timer.IsEnabled)
                _Timer.Stop();
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

        // Close window when escape key is pressed
        private void OnPreviewKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
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

        private Style GetApplicationStyle<T>()
        {
            Style tempStyle;
            if (Application.Current.Resources.Contains(typeof(T)))
                tempStyle = new Style(typeof(T), (Style)Application.Current.Resources[typeof(T)]);
            else
                tempStyle = new Style(typeof(T));

            return tempStyle;
        }
    }
}