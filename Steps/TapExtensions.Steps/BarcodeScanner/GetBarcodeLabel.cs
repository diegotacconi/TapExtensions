using System;
using OpenTap;
using TapExtensions.Interfaces.BarcodeScanner;

namespace TapExtensions.Steps.BarcodeScanner
{
    [Display("GetBarcodeLabel",
        Groups: new[] { "TapExtensions", "Steps", "BarcodeScanner" })]
    public class GetBarcodeLabel : TestStep
    {
        [Display("BarcodeScanner")]
        public IBarcodeScanner BarcodeScanner { get; set; }

        public override void Run()
        {
            try
            {
                var (serialNumber, productCode) = BarcodeScanner.GetBarcodeLabel();
                Log.Debug($"productCode  = '{productCode}'");
                Log.Debug($"serialNumber = '{serialNumber}'");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                // Publish(Name, false, true, true, "bool");
            }
        }
    }
}