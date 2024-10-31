using System;
using OpenTap;
using TapExtensions.Interfaces.BarcodeScanner;

namespace TapExtensions.Steps.BarcodeScanner
{
    [Display("GetSerialNumberAndProductCode",
        Groups: new[] { "TapExtensions", "Steps", "BarcodeScanner" })]
    public class GetSerialNumberAndProductCode : TestStep
    {
        [Display("BarcodeScanner")] public IBarcodeScanner BarcodeScanner { get; set; }

        public override void Run()
        {
            try
            {
                var (serialNumber, productCode) = BarcodeScanner.GetSerialNumberAndProductCode();
                Log.Debug($"productCode  = '{productCode}'");
                Log.Debug($"serialNumber = '{serialNumber}'");
                UpgradeVerdict(Verdict.Pass);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }
    }
}