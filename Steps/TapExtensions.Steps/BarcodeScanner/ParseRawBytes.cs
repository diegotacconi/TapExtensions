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
                // Example parsing the barcode raw bytes
                var rawBytes = SimulateGetRawBytes();
                Log.Debug(AsciiBytesToString(rawBytes));




            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        private static byte[] SimulateGetRawBytes()
        {
            // [)>{1E}06{1D}1P089659A.X12{1D}SL1194215398{1D}1T1RK6EUCJ{1E}{04}
            var bytes = new byte[]
            {
                0x5B, 0x29, 0x3E,
                0x1E, 0x30, 0x36,
                0x1D, 0x31, 0x50, 0x30, 0x38, 0x39, 0x36, 0x35, 0x39, 0x41, 0x2E, 0x58, 0x31, 0x32,
                0x1D, 0x53, 0x4C, 0x31, 0x31, 0x39, 0x34, 0x32, 0x31, 0x35, 0x33, 0x39, 0x38,
                0x1D, 0x31, 0x54, 0x31, 0x52, 0x4B, 0x36, 0x45, 0x55, 0x43, 0x4A,
                0x1E, 0x04
            };

            return bytes;
        }

        private static string AsciiBytesToString(byte[] bytes)
        {
            var msg = new StringBuilder();
            if (bytes != null && bytes.Length != 0)
            {
                foreach (var c in bytes)
                {
                    var j = c;
                    if (j >= 0x20 && j <= 0x7E)
                    {
                        msg.Append((char)j);
                    }
                    else
                    {
                        msg.Append("{" + c.ToString("X2") + "}");
                    }
                }
            }
            return msg.ToString();
        }
    }
}