using System;
using System.Collections.Generic;
using System.IO;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Ssh
{
    [Display("UploadFiles",
        Groups: new[] { "TapExtensions", "Steps", "Ssh" })]
    public class UploadFiles : TestStep
    {
        #region Settings

        [Display("Dut", Order: 1)] public ISecureShell Dut { get; set; }

        public class FilePair : ValidatingObject
        {
            [Display("Local File (Source)", Order: 1,
                Description: "Full path of the local file location")]
            [FilePath]
            public string LocalFile
            {
                get => _localFile;
                set => _localFile = !string.IsNullOrWhiteSpace(value) ? Path.GetFullPath(value) : "";
            }

            private string _localFile;

            [Display("Remote File (Destination)", Order: 2,
                Description: "Full path to the remote file location")]
            public string RemoteFile { get; set; }

            public FilePair()
            {
                // Validation rules
                Rules.Add(() => !string.IsNullOrWhiteSpace(LocalFile),
                    "Cannot be empty", nameof(LocalFile));
                Rules.Add(() => !string.IsNullOrWhiteSpace(RemoteFile),
                    "Cannot be empty", nameof(RemoteFile));
                Rules.Add(() => LocalFile?.IndexOfAny(Path.GetInvalidPathChars()) < 0,
                    "Not valid", nameof(LocalFile));
                Rules.Add(() => RemoteFile?.IndexOfAny(Path.GetInvalidPathChars()) < 0,
                    "Not valid", nameof(RemoteFile));
            }
        }

        [Display("Files", Order: 5)] public List<FilePair> Files { get; set; }

        #endregion

        public UploadFiles()
        {
            // Default values
            Files = new List<FilePair>
            {
                new FilePair { LocalFile = @"C:\Windows\Web\Screen\img102.jpg", RemoteFile = "/tmp/img102.jpg" },
                new FilePair { LocalFile = @"C:\Windows\Web\Screen\img103.png", RemoteFile = "/tmp/img103.png" }
            };
        }

        public override void Run()
        {
            try
            {
                var files = new List<(string, string)>();
                foreach (var x in Files)
                    files.Add((x.LocalFile, x.RemoteFile));

                Dut.UploadFiles(files);
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