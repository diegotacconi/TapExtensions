using OpenTap;
using System.IO;

namespace TapExtensions.Interfaces.Results
{
    public interface IFileAttachment : IResultListener
    {
        void AddFile(string filePath);
        void AddStream(string fileName, Stream fileStream);
    }
}