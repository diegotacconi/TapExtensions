using OpenTap;

namespace TapExtensions.Interfaces.BarcodeScanner
{
    public interface IBarcodeScanner : IInstrument
    {
        byte[] GetRawBytes();
        (string serialNumber, string productCode) GetSerialNumberAndProductCode();
    }
}