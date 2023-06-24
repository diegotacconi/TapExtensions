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
        }

        private void SetDialogWindowControls()
        {
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