using System;
using OpenTap;
using TapExtensions.Interfaces.BarcodeScanner;

namespace TapExtensions.Instruments.BarcodeScanner
{
    public abstract class BarcodeScannerBase : Instrument, IBarcodeScanner
    {
        #region Settings

        [Display("Retry", Order: 10,
            Description: "Maximum number of iteration attempts to retry scanning the barcode label.")]
        public Enabled<int> MaxIterationCount { get; set; }

        #endregion

        protected BarcodeScannerBase()
        {
            // Default values
            MaxIterationCount = new Enabled<int> { IsEnabled = true, Value = 3 };

            // Validation rules
            Rules.Add(() => MaxIterationCount.Value > 0,
                "Must be greater than zero", nameof(MaxIterationCount));
        }

        public virtual byte[] GetRawBytes()
        {
            throw new NotImplementedException("Please override in subclasses");
        }

        public virtual (string serialNumber, string productCode) GetSerialNumberAndProductCode()
        {
            var serialNumber = "";
            var productCode = "";
            var maxCount = MaxIterationCount.IsEnabled ? MaxIterationCount.Value : 1;

            // Retry loop
            for (var iteration = 1; iteration <= maxCount; iteration++)
            {
                try
                {
                    if (iteration > 1)
                        Log.Warning($"Retrying attempt {iteration} of {maxCount} ...");

                    // Try to scan the barcode label
                    var rawBytes = GetRawBytes();

                    // Parse the barcode label
                    productCode = BarcodeLabelUtility.GetProductCode(rawBytes);
                    serialNumber = BarcodeLabelUtility.GetSerialNumber(rawBytes);

                    // Exit loop if no exceptions
                    break;
                }
                catch (Exception ex)
                {
                    if (iteration < maxCount)
                        Log.Debug($"IgnoreException: {ex.Message}");
                    else
                        throw;
                }
            }

            return (serialNumber, productCode);
        }
    }
}