using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TapExtensions.Interfaces.Gui;
using TapExtensions.Shared;

namespace TapExtensions.Gui.Wpf.Dialogs
{
    // ReSharper disable once RedundantExtendsListEntry
    internal partial class BarcodeDialogWpf : Window
    {
        internal string WindowMessage { get; set; } = "";
        internal string WindowPicture { get; set; } = "";
        internal int Timeout { get; set; } = 0;
        internal bool IsSerialNumberVisible { get; set; } = false;
        internal bool IsProductCodeVisible { get; set; } = false;
        internal EInputButtons Buttons { get; set; } = EInputButtons.StartCancel;
        internal double WindowFontSize { get; set; } = 0;
        internal double WindowMaxWidth { get; set; } = 0;
        internal double WindowMaxHeight { get; set; } = 0;
        internal bool IsWindowResizable { get; set; } = false;
        internal EBorderStyle BorderStyle { get; set; } = EBorderStyle.None;

        private DateTime _startTime;
        private DispatcherTimer _timer;

        internal BarcodeDialogWpf(Window windowOwner = null)
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
            TextBoxSerialNumber.Focus();

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

            if (IsSerialNumberVisible)
            {
                LabelSerialNumber.Visibility = Visibility.Visible;
                TextBoxSerialNumber.Visibility = Visibility.Visible;
            }
            else
            {
                LabelSerialNumber.Visibility = Visibility.Collapsed;
                TextBoxSerialNumber.Visibility = Visibility.Collapsed;
            }

            if (IsProductCodeVisible)
            {
                LabelProductCode.Visibility = Visibility.Visible;
                TextBoxProductCode.Visibility = Visibility.Visible;
            }
            else
            {
                LabelProductCode.Visibility = Visibility.Collapsed;
                TextBoxProductCode.Visibility = Visibility.Collapsed;
            }

            switch (Buttons)
            {
                case EInputButtons.StartCancel:
                    ButtonStart.Content = "Start";
                    ButtonCancel.Content = "Cancel";
                    break;

                case EInputButtons.OkCancel:
                    ButtonStart.Content = "OK";
                    ButtonCancel.Content = "Cancel";
                    break;

                case EInputButtons.YesNo:
                    ButtonStart.Content = "Yes";
                    ButtonCancel.Content = "No";
                    break;

                default:
                    throw new ArgumentException(
                        $"Case not found for {nameof(Buttons)}={Buttons}");
            }
        }

        private void SetDialogWindowProperties()
        {
            if (WindowFontSize > 0)
                DialogWindow.FontSize = WindowFontSize;

            if (WindowMaxWidth > 0)
                DialogWindow.MaxWidth = WindowMaxWidth;

            if (WindowMaxHeight > 0)
                DialogWindow.MaxHeight = WindowMaxHeight;

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

        private void OnStartButtonClick(object sender, RoutedEventArgs e)
        {
            // Check and update SerialNumber
            if (TextBoxSerialNumber.Visibility == Visibility.Visible)
                //Dut.SerialNumber = TextBoxSerialNumber.Text.ToUpper().Trim();

                // Check and update ProductCode
                if (TextBoxProductCode.Visibility == Visibility.Visible)
                    //Dut.ProductCode = TextBoxProductCode.Text.ToUpper().Trim();

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

        private void StartTimer()
        {
            _startTime = DateTime.UtcNow;
            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 100); // Set interval to 100 milliseconds
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

        private Style GetApplicationStyle<T>()
        {
            Style tempStyle;
            if (Application.Current.Resources.Contains(typeof(T)))
                tempStyle = new Style(typeof(T), (Style)Application.Current.Resources[typeof(T)]);
            else
                tempStyle = new Style(typeof(T));

            return tempStyle;
        }

        private void OnTextBoxSerialNumberChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(TextBoxSerialNumber.Text))
                return;

            // Try to parse barcode from hand-held scanner, when configured in keyboard emulation mode
            var bytes = Encoding.ASCII.GetBytes(TextBoxSerialNumber.Text);
            var serialNumber = BarcodeLabelParser.GetSerialNumber(bytes);
            if (!string.IsNullOrEmpty(serialNumber))
                TextBoxSerialNumber.Text = serialNumber;

            var productCode = BarcodeLabelParser.GetProductCode(bytes);
            if (!string.IsNullOrEmpty(productCode))
                TextBoxProductCode.Text = productCode;
        }

        private void OnTextBoxProductCodeChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(TextBoxProductCode.Text))
                return;

            // Try to parse barcode from hand-held scanner, when configured in keyboard emulation mode
            var bytes = Encoding.ASCII.GetBytes(TextBoxProductCode.Text);
            var serialNumber = BarcodeLabelParser.GetSerialNumber(bytes);
            if (!string.IsNullOrEmpty(serialNumber))
                TextBoxSerialNumber.Text = serialNumber;

            var productCode = BarcodeLabelParser.GetProductCode(bytes);
            if (!string.IsNullOrEmpty(productCode))
                TextBoxProductCode.Text = productCode;
        }
    }
}