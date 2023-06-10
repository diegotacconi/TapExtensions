using System;
using System.Text;
using OpenTap;

namespace TapExtensions.Steps.BarcodeScanner
{
    [Display("TestingBarcodeLabelUtility",
    Groups: new[] { "TapExtensions", "Steps", "BarcodeScanner" })]
    public class TestingBarcodeLabelUtility : TestStep
    {
        public override void Run()
        {
            try
            {
                // var barcodeString = "[)>[RS]06[GS]1P089659A.X12[GS]SL1194215398[GS]1T1RK6EUCJ[RS][EOT]";
                // var rawBytes = Encoding.ASCII.GetBytes(barcodeString);

                var rawBytes = new byte[] { 0x5B, 0x29, 0x3E, 0x1E, 0x30, 0x36, 0x1D, 0x31, 0x50, 0x30, 0x38, 0x39, 0x36, 0x35, 0x39, 0x41, 0x2E, 0x58, 0x31, 0x32, 0x1D, 0x53, 0x4C, 0x31, 0x31, 0x39, 0x34, 0x32, 0x31, 0x35, 0x33, 0x39, 0x38, 0x1D, 0x31, 0x54, 0x31, 0x52, 0x4B, 0x36, 0x45, 0x55, 0x43, 0x4A, 0x1E, 0x04 };

                var hex = new StringBuilder();
                var ascii = new StringBuilder();
                foreach (var c in rawBytes)
                {
                    hex.Append(c.ToString("X2") + " ");

                    var j = c;
                    if (j >= 0x20 && j <= 0x7E)
                        ascii.Append((char)j + "  ");
                    else
                        ascii.Append('.' + "  ");
                }
                Log.Debug($"Hex:   {hex}");
                Log.Debug($"Ascii: {ascii}");




                // Parse the barcode label
                var productCode = BarcodeLabelUtility.GetProductCode(rawBytes);
                var serialNumber = BarcodeLabelUtility.GetSerialNumber(rawBytes);

                Log.Debug($"productCode  = '{productCode}'");
                Log.Debug($"serialNumber = '{serialNumber}'");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
    }
}