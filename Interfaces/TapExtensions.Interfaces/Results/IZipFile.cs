using OpenTap;
using System.IO;

namespace TapExtensions.Interfaces.Results
{
    public interface IZipFile : IResultListener
    {
        void AddFile(string filePath);
        void AddStream(string fileName, Stream fileStream);
    }
}