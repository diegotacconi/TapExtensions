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
                "Must be greater than zero", nameof(MaxIterationCount.Value));
        }

        public virtual byte[] GetRawBytes()
        {
            throw new NotImplementedException();
        }

        public virtual (string serialNumber, string productCode) GetBarcodeLabel()
        {
            var serialNumber = "";
            var productCode = "";
            var keepOnRetrying = true;
            var iteration = 0;

            // Retry loop
            do
            {
                try
                {
                    iteration++;

                    if (iteration > 1)
                        Log.Warning($"Retrying attempt {iteration} of {MaxIterationCount.Value} ...");

                    // Try to scan the barcode label
                    var rawBytes = GetRawBytes();

                    // Parse the barcode label
                    serialNumber = BarcodeUtility.GetSerialNumber(rawBytes);
                    productCode = BarcodeUtility.GetProductCode(rawBytes);

                    // Exit loop if no exceptions
                    keepOnRetrying = false;
                }
                catch (Exception ex)
                {
                    if (!MaxIterationCount.IsEnabled)
                        throw;

                    if (iteration >= MaxIterationCount.Value)
                        throw;

                    Log.Warning(ex.Message);
                }
            } while (keepOnRetrying);

            return (serialNumber, productCode);
        }
    }
}