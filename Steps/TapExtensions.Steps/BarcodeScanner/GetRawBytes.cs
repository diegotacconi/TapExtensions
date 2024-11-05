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
        [Display("BarcodeScanner")] public IBarcodeScanner BarcodeScanner { get; set; }

        public override void Run()
        {
            try
            {
                var rawBytes = BarcodeScanner.GetRawBytes();
                Log.Info(AsciiBytesToString(rawBytes));
                UpgradeVerdict(Verdict.Pass);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }

        private static string AsciiBytesToString(byte[] bytes)
        {
            var msg = new StringBuilder();
            if (bytes != null && bytes.Length != 0)
                foreach (var c in bytes)
                    if (c >= 0x20 && c <= 0x7E)
                        msg.Append((char)c);
                    else
                        msg.Append("{" + c.ToString("X2") + "}");

            return msg.ToString();
        }
    }
}