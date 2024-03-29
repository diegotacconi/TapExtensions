using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Ssh
{
    [Display("SshDownloadFiles",
        Groups: new[] { "TapExtensions", "Steps", "Ssh" })]
    public class SshDownloadFiles : TestStep
    {
        #region Settings

        [Display("Dut", Order: 1)] public ISecureShell Dut { get; set; }

        public class RemoteAndLocalFilePair : ValidatingObject
        {
            [Display("Remote Filename (Source)", Order: 1,
                Description: "Full path of the remote file location")]
            public string RemoteFilename { get; set; }

            [Display("Local Filename (Destination)", Order: 2,
                Description: "Full path to the local file location")]
            public string LocalFilename { get; set; }

            public RemoteAndLocalFilePair()
            {
                // Validation rules
                Rules.Add(() => !string.IsNullOrWhiteSpace(RemoteFilename),
                    "Filename cannot be empty", nameof(RemoteFilename));
                Rules.Add(() => !string.IsNullOrWhiteSpace(LocalFilename),
                    "Filename cannot be empty", nameof(LocalFilename));
                Rules.Add(() => RemoteFilename?.IndexOfAny(Path.GetInvalidPathChars()) < 0,
                    "Not valid", nameof(RemoteFilename));
                Rules.Add(() => LocalFilename?.IndexOfAny(Path.GetInvalidPathChars()) < 0,
                    "Not valid", nameof(LocalFilename));
            }
        }

        [Display("Files", Order: 5)] public List<RemoteAndLocalFilePair> Files { get; set; }

        #endregion

        public SshDownloadFiles()
        {
            // Default values
            Files = new List<RemoteAndLocalFilePair>
            {
                new RemoteAndLocalFilePair
                {
                    RemoteFilename = "/tmp/img100.jpg",
                    LocalFilename = @"C:\Temp\img100.jpg"
                }
            };
        }

        public override void Run()
        {
            try
            {
                var files = Files.Select(file => (file.RemoteFilename, file.LocalFilename)).ToList();
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