using System;
using System.Text;
using OpenTap;
using TapExtensions.Interfaces.BarcodeScanner;

namespace TapExtensions.Steps.BarcodeScanner
{
    [Display("GetRawBytes",
        Groups: new[] { "TapExtensions", "Steps", "BarcodeScanner" })]
    public class GetRawBytes : TestStep
    {
        [Display("BarcodeScanner")]
        public IBarcodeScanner BarcodeScanner { get; set; }

        public override void Run()
        {
            try
            {
                var rawBytes = BarcodeScanner.GetRawBytes();
                Log.Debug(AsciiBytesToString(rawBytes));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
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