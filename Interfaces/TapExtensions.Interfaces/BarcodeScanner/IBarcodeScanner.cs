using OpenTap;

namespace TapExtensions.Interfaces.BarcodeScanner
{
    public interface IBarcodeScanner : IInstrument
    {
        byte[] GetRawBytes();
    }
}