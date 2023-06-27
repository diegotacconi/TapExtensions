using System;
using System.IO;
using OpenTap;
using TapExtensions.Interfaces.Results;

namespace TapExtensions.Steps.Results
{
    [Display("AddFileToZip", Groups: new[] { "TapExtensions", "Steps", "Results" })]
    public class AddFileToZip : TestStep
    {
        [Display("Result Listener", Order: 1,
            Description: "Reference to a result listener, such as 'Zip File'.")]
        public IZipFile ZipFileListener { get; set; }

        [Display("File To Add", Order: 2,
            Description: "Full path of the file to be added.")]
        [FilePath]
        public string FullPath
        {
            get => _fullPath;
            set => _fullPath = !string.IsNullOrWhiteSpace(value) ? Path.GetFullPath(value) : "";
        }
        private string _fullPath;

        public AddFileToZip()
        {
            FullPath = @"C:\Windows\Web\Screen\img103.png";
            Rules.Add(() => System.IO.File.Exists(FullPath), "File not found", nameof(FullPath));
        }

        public override void Run()
        {
            try
            {
                ZipFileListener.AddFile(FullPath);
                // Publish(Name, result, true, true, "bool");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                // Publish(Name, false, true, true, "bool");
            }
        }
    }
}