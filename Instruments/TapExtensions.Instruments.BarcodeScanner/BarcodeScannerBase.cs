using System;
using OpenTap;
using TapExtensions.Interfaces.BarcodeScanner;

namespace TapExtensions.Instruments.BarcodeScanner
{
    public abstract class BarcodeScannerBase : Instrument, IBarcodeScanner
    {
        public virtual byte[] GetRawBytes()
        {
            throw new NotImplementedException();
        }

        // ToDo: implement parsing of barcode label
    }
}