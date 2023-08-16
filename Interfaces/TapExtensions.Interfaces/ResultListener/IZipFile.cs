using System.IO;
using OpenTap;

namespace TapExtensions.Interfaces.ResultListener
{
    public interface IZipFile : IResultListener
    {
        void AddExistingFile(string fileName);
        void AddNewFile(string fileName, string fileContents);
        void AddNewFile(string fileName, MemoryStream fileContents);
    }
}