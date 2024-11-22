using OpenTap;

namespace TapExtensions.Interfaces.BarcodeScanner
{
    public interface IBarcodeScanner : IInstrument
    {
        byte[] GetRawLabelContents();
        (string serialNumber, string productCode) GetSerialNumberAndProductCode();
    }
}