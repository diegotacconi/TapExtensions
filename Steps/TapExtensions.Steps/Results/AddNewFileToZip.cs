using OpenTap;
using TapExtensions.Interfaces.Results;

namespace TapExtensions.Steps.Results
{
    [Display("AddNewFileToZip", Groups: new[] { "TapExtensions", "Steps", "Results" })]
    public class AddNewFileToZip : TestStep
    {
        [Display("Result Listener", Order: 1,
            Description: "Reference to a result listener, such as 'Zip File'.")]
        public IZipFile ZipFileListener { get; set; }

        [Display("File Name", Order: 2,
            Description: "Full path of the file to be added.")]
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
        }

        public override void Run()
        {
            ZipFileListener.AddNewFile(FileName, FileContents);
        }
    }
}