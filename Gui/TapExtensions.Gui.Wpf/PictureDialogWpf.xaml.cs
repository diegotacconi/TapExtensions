using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TapExtensions.Interfaces.Gui;

namespace TapExtensions.Gui.Wpf
{
    // ReSharper disable once RedundantExtendsListEntry
    internal partial class PictureDialogWpf : Window
    {
        internal string WindowTitle { get; set; } = "";
        internal string WindowMessage { get; set; } = "";
        internal string WindowPicture { get; set; } = "";
        internal EInputButtons Buttons { get; set; } = EInputButtons.OkCancel;
        internal double WindowFontSize { get; set; } = 0;
        internal double WindowMaxWidth { get; set; } = 0;
        internal double WindowMaxHeight { get; set; } = 0;
        internal bool IsWindowResizable { get; set; } = false;
        public EBorderStyle BorderStyle { get; set; } = EBorderStyle.None;

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
            // Change style
            DialogWindow.Foreground = new SolidColorBrush(Colors.White);
            DialogWindow.Background = new SolidColorBrush(Color.FromRgb(27, 27, 31));
            switch (BorderStyle)
            {
                case EBorderStyle.None:
                    MainBorder.BorderThickness = new Thickness(0);
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

            // Change title
            if (!string.IsNullOrWhiteSpace(WindowTitle))
                DialogWindow.Title = WindowTitle;

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

            // Call the base class to show the dialog window
            return ShowDialog();
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Close window when escape key is pressed
        private void OnPreviewKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        // Resize window to default, on mouse double-click
        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (IsWindowResizable)
                SizeToContent = SizeToContent.WidthAndHeight;
        }
    }
}