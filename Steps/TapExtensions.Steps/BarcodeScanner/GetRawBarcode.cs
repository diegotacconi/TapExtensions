using System;
using OpenTap;
using TapExtensions.Interfaces.BarcodeScanner;

namespace TapExtensions.Steps.BarcodeScanner
{
    [Display("GetRawBarcode",
        Groups: new[] {"TapExtensions", "Steps", "BarcodeScanner"})]
    public class GetRawBarcode : TestStep
    {
        [Display("BarcodeScanner", Group: "Instruments",
            Description: "Barcode Scanner instrument interface")]
        public IBarcodeScanner BarcodeScanner { get; set; }

        public override void Run()
        {
            try
            {
                if (!BarcodeScanner.IsConnected)
                    throw new InvalidOperationException("Barcode Scanner is not connected");

                BarcodeScanner.GetRawData();

                // Publish(Name, true, true, true, "bool");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                // Publish(Name, false, true, true, "bool");
            }
        }
    }
}