using System;
using System.Text;
using OpenTap;

namespace TapExtensions.Steps.BarcodeScanner
{
    [Display("ParseRawBytes",
        Groups: new[] { "TapExtensions", "Steps", "BarcodeScanner" })]
    public class ParseRawBytes : TestStep
    {
        public override void Run()
        {
            try
            {
                // Example parsing the barcode row bytes







            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
    }
}