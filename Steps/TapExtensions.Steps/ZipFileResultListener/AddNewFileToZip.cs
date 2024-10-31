using System;
using System.IO;
using OpenTap;
using TapExtensions.Interfaces.ResultListener;

namespace TapExtensions.Steps.ZipFileResultListener
{
    [Display("AddNewFileToZip",
        Groups: new[] { "TapExtensions", "Steps", "ZipFileResultListener" },
        Description:
        "Example of how to add a new file to the zip file created by the 'Zip File' result listener")]
    public class AddNewFileToZip : TestStep
    {
        [Display("Result Listener", Order: 1,
            Description: "Reference to a result listener, such as 'Zip File'.")]
        public IZipFile ZipFileResultListener { get; set; }

        [Display("File Name", Order: 2,
            Description: "File name of the file to be added.")]
        public string FileName { get; set; }

        [Display("File Contents", Order: 3)]
        [Layout(LayoutMode.Normal, 4, 8)]
        public string FileContents { get; set; }

        public AddNewFileToZip()
        {
            FileName = "example.txt";
            FileContents =
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit, " +
                "sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
                "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris " +
                "nisi ut aliquip ex ea commodo consequat.";
            Rules.Add(() => !string.IsNullOrWhiteSpace(FileName), "cannot be empty", nameof(FileName));
            Rules.Add(() => FileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0, "Not valid", nameof(FileName));
            Rules.Add(() => !string.IsNullOrWhiteSpace(FileContents), "cannot be empty", nameof(FileContents));
        }

        public override void Run()
        {
            try
            {
                ZipFileResultListener.AddNewFile(FileName, FileContents);
                UpgradeVerdict(Verdict.Pass);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }
    }
}