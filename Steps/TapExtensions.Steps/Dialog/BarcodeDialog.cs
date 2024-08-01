using System;
using System.IO;
using System.Text.RegularExpressions;
using OpenTap;
using TapExtensions.Gui.Wpf.Dialogs;
using TapExtensions.Interfaces.Gui;

namespace TapExtensions.Steps.Dialog
{
    [Display("BarcodeDialog", Groups: new[] { "TapExtensions", "Steps", "Dialog" })]
    public class BarcodeDialog : TestStep
    {
        #region Settings

        [Display("Title", Order: 1, Group: "Visible Controls",
            Description: "The title of the dialog window.")]
        public string Title { get; set; }

        [Display("Message", Order: 2, Group: "Visible Controls",
            Description: "The message shown to the user.")]
        [Layout(LayoutMode.Normal, 2, 6)]
        public string Message { get; set; }

        [Display("Picture", Order: 3, Group: "Visible Controls",
            Description: "Path to the picture file such as 'C:\\image.jpg'.")]
        [FilePath]
        public string Picture
        {
            get => _fullPath;
            set => _fullPath = !string.IsNullOrWhiteSpace(value) ? Path.GetFullPath(value) : "";
        }

        private string _fullPath;

        [Display("Serial Number", Order: 4, Group: "Visible Controls")]
        public bool IsSerialNumberVisible { get; set; }

        [Display("Product Code", Order: 5, Group: "Visible Controls")]
        public bool IsProductCodeVisible { get; set; }

        [Display("Font Size", Order: 10, Group: "Dialog Window Properties", Collapsed: true,
            Description: "The size of the text.")]
        public Enabled<double> FontSize { get; set; }

        [Display("Max Width", Order: 11, Group: "Dialog Window Properties", Collapsed: true,
            Description: "The maximum width of the dialog window, in pixels.")]
        public Enabled<double> MaxWidth { get; set; }

        [Display("Max Height", Order: 12, Group: "Dialog Window Properties", Collapsed: true,
            Description: "The maximum height of the dialog window, in pixels.")]
        public Enabled<double> MaxHeight { get; set; }

        [Display("Is Resizable", Order: 13, Group: "Dialog Window Properties", Collapsed: true,
            Description: "Specifies whether the dialog window can be resized.")]
        public bool IsResizable { get; set; }

        [Display("Border Color", Order: 14, Group: "Dialog Window Properties", Collapsed: true,
            Description: "Color as a hexadecimal string:\r" +
            "Green = Rgb(46, 204, 113) = #2ECC71\r" +
            "Yellow = Rgb(241, 196, 15) = #F1C40F\r" +
            "Orange = Rgb(230, 126, 34) = #E67E22\r" +
            "Red = Rgb(231, 76, 60) = #E74C3C\r" +
            "Blue = Rgb(76, 142, 255) = #4C8EFF")]
        public Enabled<string> BorderColor { get; set; }

        [Display("Check Serial Number", Order: 20, Group: "Post Actions", Collapsed: true,
            Description: "The regular expression to apply to the Serial Number.")]
        public Enabled<string> SerialNumberRegularExpression { get; set; }

        [Display("Check Product Code", Order: 21, Group: "Post Actions", Collapsed: true,
            Description: "The regular expression to apply to the Product Code.")]
        public Enabled<string> ProductCodeRegularExpression { get; set; }

        #endregion

        public BarcodeDialog()
        {
            // Default values
            Title = "Please enter the barcode label";
            Message = "";
            Picture = "";
            IsSerialNumberVisible = true;
            IsProductCodeVisible = true;
            FontSize = new Enabled<double> { IsEnabled = false, Value = 14 };
            MaxWidth = new Enabled<double> { IsEnabled = false, Value = 450 };
            MaxHeight = new Enabled<double> { IsEnabled = false, Value = 450 };
            IsResizable = false;
            BorderColor = new Enabled<string> { IsEnabled = false, Value = "#F1C40F" };
            SerialNumberRegularExpression = new Enabled<string> { IsEnabled = false, Value = "^[A-Z0-9]{11}$" };
            ProductCodeRegularExpression = new Enabled<string> { IsEnabled = false, Value = "^[A-Z0-9.]{11}$" };
        }

        public override void Run()
        {
            try
            {
                // Check if picture file exists
                if (!string.IsNullOrWhiteSpace(Picture) && !File.Exists(Picture))
                    throw new FileNotFoundException($"Cannot find picture file {Picture}");

                // Show dialog window
                IGui gui = new BarcodeDialogGui
                {
                    Title = Title,
                    Message = Message,
                    Picture = Picture,
                    IsSerialNumberVisible = IsSerialNumberVisible,
                    IsProductCodeVisible = IsProductCodeVisible,
                    Buttons = EInputButtons.StartCancel,
                    FontSize = FontSize.IsEnabled ? FontSize.Value : 0,
                    MaxWidth = MaxWidth.IsEnabled ? MaxWidth.Value : 0,
                    MaxHeight = MaxHeight.IsEnabled ? MaxHeight.Value : 0,
                    IsResizable = IsResizable,
                    BorderColor = BorderColor.IsEnabled ? BorderColor.Value : "",
                };
                var okayButton = gui.ShowDialog();

                // Check response from the user
                if (okayButton)
                    Log.Debug("User approved the dialog window");
                else
                    Log.Warning("User canceled the dialog window");

                UpgradeVerdict(okayButton ? Verdict.Pass : Verdict.Fail);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }

        private void CheckSerialNumber(string serialNumber)
        {
            if (IsSerialNumberVisible && SerialNumberRegularExpression.IsEnabled)
            {
                var serialNumberRegex = new Regex(SerialNumberRegularExpression.Value);
                if (!serialNumberRegex.IsMatch(serialNumber))
                    throw new InvalidOperationException(
                        $"The SerialNumber of '{serialNumber}' does not match " +
                        $"the RegularExpression of '{SerialNumberRegularExpression.Value}'");
            }
        }

        private void CheckProductCode(string productCode)
        {
            if (IsProductCodeVisible && ProductCodeRegularExpression.IsEnabled)
            {
                var productCodeRegex = new Regex(ProductCodeRegularExpression.Value);
                if (!productCodeRegex.IsMatch(productCode))
                    throw new InvalidOperationException(
                        $"The ProductCode of '{productCode}' does not match " +
                        $"the RegularExpression of '{ProductCodeRegularExpression.Value}'");
            }
        }
    }
}