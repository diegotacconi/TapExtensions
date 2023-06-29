using System.IO;
using OpenTap;
using TapExtensions.Interfaces.Results;

namespace TapExtensions.Steps.Results
{
    [Display("AddExistingFileToZip", Groups: new[] { "TapExtensions", "Steps", "Results" })]
    public class AddExistingFileToZip : TestStep
    {
        [Display("Result Listener", Order: 1,
            Description: "Reference to a result listener, such as 'Zip File'.")]
        public IZipFile ZipFileListener { get; set; }

        [Display("File Name", Order: 2,
            Description: "Full path of the file to be added.")]
        [FilePath]
        public string FileName
        {
            get => _fileName;
            set => _fileName = !string.IsNullOrWhiteSpace(value) ? Path.GetFullPath(value) : "";
        }

        private string _fileName;

        public AddExistingFileToZip()
        {
            FileName = @"C:\Windows\Web\Screen\img103.png";
            Rules.Add(() => File.Exists(FileName), "File not found", nameof(FileName));
        }

        public override void Run()
        {
            ZipFileListener.AddExistingFile(FileName);
        }
    }
}