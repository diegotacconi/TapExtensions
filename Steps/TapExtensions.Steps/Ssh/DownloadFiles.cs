using System;
using System.Collections.Generic;
using System.IO;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Ssh
{
    [Display("DownloadFiles",
        Groups: new[] { "TapExtensions", "Steps", "Ssh" })]
    public class DownloadFiles : TestStep
    {
        #region Settings

        [Display("Dut", Order: 1)] public ISecureShell Dut { get; set; }

        public class FilePair : ValidatingObject
        {
            [Display("Remote File (Source)", Order: 1,
                Description: "Full path of the remote file location")]
            public string RemoteFile { get; set; }

            [Display("Local File (Destination)", Order: 2,
                Description: "Full path to the local file location")]
            public string LocalFile { get; set; }

            public FilePair()
            {
                // Validation rules
                Rules.Add(() => !string.IsNullOrWhiteSpace(RemoteFile),
                    "Cannot be empty", nameof(RemoteFile));
                Rules.Add(() => !string.IsNullOrWhiteSpace(LocalFile),
                    "Cannot be empty", nameof(LocalFile));
                Rules.Add(() => RemoteFile?.IndexOfAny(Path.GetInvalidPathChars()) < 0,
                    "Not valid", nameof(RemoteFile));
                Rules.Add(() => LocalFile?.IndexOfAny(Path.GetInvalidPathChars()) < 0,
                    "Not valid", nameof(LocalFile));
            }
        }

        [Display("Files", Order: 5)] public List<FilePair> Files { get; set; }

        #endregion

        public DownloadFiles()
        {
            // Default values
            Files = new List<FilePair>
            {
                new FilePair { RemoteFile = "/tmp/img102.jpg", LocalFile = @"C:\Temp\img102.jpg" },
                new FilePair { RemoteFile = "/tmp/img103.png", LocalFile = @"C:\Temp\img103.png" }
            };
        }

        public override void Run()
        {
            try
            {
                var files = new List<(string, string)>();
                foreach (var x in Files)
                    files.Add((x.RemoteFile, x.LocalFile));

                Dut.DownloadFiles(files);
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