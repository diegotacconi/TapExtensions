﻿using System;
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
        internal string WindowTitle { get; set; } = "";
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
        internal string BorderColor { get; set; } = "";

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
            SetDialogWindowBorders();
            SetDialogWindowControls();
            SetDialogWindowProperties();
            SerialNumberTextBox.Focus();

            // Start the countdown timer
            if (Timeout > 0)
                StartTimer();

            // Call the base class to show the dialog window
            return ShowDialog();
        }

        private void SetDialogWindowBorders()
        {
            if (string.IsNullOrWhiteSpace(BorderColor))
            {
                BorderWithStripes.Margin = new Thickness(0);
                BorderWithStripes.BorderThickness = new Thickness(0);
                BorderWithStripes.Padding = new Thickness(0);
            }
            else
            {
                var color = (Color)ColorConverter.ConvertFromString(BorderColor);
                Stripe1.Color = color;
                Stripe2.Color = color;
            }
        }

        private void SetDialogWindowControls()
        {
            // Show title
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

            // Show serial number
            if (IsSerialNumberVisible)
            {
                SerialNumberLabel.Visibility = Visibility.Visible;
                SerialNumberTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                SerialNumberLabel.Visibility = Visibility.Collapsed;
                SerialNumberTextBox.Visibility = Visibility.Collapsed;
            }

            // Show product code
            if (IsProductCodeVisible)
            {
                ProductCodeLabel.Visibility = Visibility.Visible;
                ProductCodeTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                ProductCodeLabel.Visibility = Visibility.Collapsed;
                ProductCodeTextBox.Visibility = Visibility.Collapsed;
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
            if (IsWindowResizable)
            {
                DialogWindow.ResizeMode = ResizeMode.CanResize;
            }
            else
            {
                DialogWindow.ResizeMode = ResizeMode.NoResize;
                ProductCodeTextBox.Width = 150;
                SerialNumberTextBox.Width = 150;
            }
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
            // if (SerialNumberTextBox.Visibility == Visibility.Visible)
            //     Dut.SerialNumber = TextBoxSerialNumber.Text.ToUpper().Trim();

            // Check and update ProductCode
            // if (ProductCodeTextBox.Visibility == Visibility.Visible)
            //     Dut.ProductCode = TextBoxProductCode.Text.ToUpper().Trim();

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

        private void OnSerialNumberTextBoxChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(SerialNumberTextBox.Text))
                return;

            // Try to parse barcode from hand-held scanner, when configured in keyboard emulation mode
            var bytes = Encoding.ASCII.GetBytes(SerialNumberTextBox.Text);
            var serialNumber = BarcodeLabelParser.GetSerialNumber(bytes);
            if (!string.IsNullOrEmpty(serialNumber))
                SerialNumberTextBox.Text = serialNumber;

            var productCode = BarcodeLabelParser.GetProductCode(bytes);
            if (!string.IsNullOrEmpty(productCode))
                ProductCodeTextBox.Text = productCode;
        }

        private void OnProductCodeTextBoxChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(ProductCodeTextBox.Text))
                return;

            // Try to parse barcode from hand-held scanner, when configured in keyboard emulation mode
            var bytes = Encoding.ASCII.GetBytes(ProductCodeTextBox.Text);
            var serialNumber = BarcodeLabelParser.GetSerialNumber(bytes);
            if (!string.IsNullOrEmpty(serialNumber))
                SerialNumberTextBox.Text = serialNumber;

            var productCode = BarcodeLabelParser.GetProductCode(bytes);
            if (!string.IsNullOrEmpty(productCode))
                ProductCodeTextBox.Text = productCode;
        }
    }
}