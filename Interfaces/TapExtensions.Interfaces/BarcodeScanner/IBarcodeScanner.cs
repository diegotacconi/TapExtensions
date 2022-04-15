using OpenTap;

namespace TapExtensions.Interfaces.BarcodeScanner
{
    public interface IBarcodeScanner : IInstrument
    {
        // string GetProductCode();

        byte[] GetRawData();

        // string GetSerialNumber();

        // void GetSerialAndProductCode(out string serialNumber, out string productCode);
    }
}