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

        [Display("Message", Order: 1, Group: "Visible Controls",
            Description: "The message shown to the user.")]
        [Layout(LayoutMode.Normal, 2, 6)]
        public string Message { get; set; }

        [Display("Picture", Order: 2, Group: "Visible Controls",
            Description: "Path to the picture file such as 'C:\\image.jpg'.")]
        [FilePath]
        public string Picture
        {
            get => _fullPath;
            set => _fullPath = !string.IsNullOrWhiteSpace(value) ? Path.GetFullPath(value) : "";
        }

        private string _fullPath;

        [Display("Serial Number", Order: 3, Group: "Visible Controls")]
        public bool IsSerialNumberVisible { get; set; }

        [Display("Product Code", Order: 4, Group: "Visible Controls")]
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

        [Display("Border Style", Order: 14, Group: "Dialog Window Properties", Collapsed: true)]
        public EBorderStyle BorderStyle { get; set; }

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
            Message = "Please enter the barcode label";
            Picture = "";
            IsSerialNumberVisible = true;
            IsProductCodeVisible = true;
            FontSize = new Enabled<double> { IsEnabled = false, Value = 14 };
            MaxWidth = new Enabled<double> { IsEnabled = false, Value = 450 };
            MaxHeight = new Enabled<double> { IsEnabled = false, Value = 450 };
            IsResizable = false;
            BorderStyle = EBorderStyle.None;
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
                    Message = Message,
                    Picture = Picture,
                    IsSerialNumberVisible = IsSerialNumberVisible,
                    IsProductCodeVisible = IsProductCodeVisible,
                    Buttons = EInputButtons.StartCancel,
                    FontSize = FontSize.IsEnabled ? FontSize.Value : 0,
                    MaxWidth = MaxWidth.IsEnabled ? MaxWidth.Value : 0,
                    MaxHeight = MaxHeight.IsEnabled ? MaxHeight.Value : 0,
                    IsResizable = IsResizable,
                    BorderStyle = BorderStyle
                };
                var result = gui.ShowDialog();

                // Check response from the user
                if (result)
                    Log.Debug("User approved the dialog window");
                else
                    Log.Warning("User canceled the dialog window");

                // CheckSerialNumber(dut.SerialNumber);

                // CheckProductCode(dut.ProductCode);

                // Publish(Name, result, true, true, "bool");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                // Publish(Name, false, true, true, "bool");
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