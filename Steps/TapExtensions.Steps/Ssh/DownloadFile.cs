using System;
using System.Collections.Generic;
using System.IO;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Ssh
{
    [Display("DownloadFile",
        Groups: new[] { "TapExtensions", "Steps", "Ssh" })]
    public class DownloadFile : TestStep
    {
        #region Settings

        [Display("Dut", Order: 1)] public ISecureShell Dut { get; set; }

        [Display("Remote File (Source)", Order: 2,
            Description: "Full path of the remote file location")]
        public string RemoteFile { get; set; }

        [Display("Local File (Destination)", Order: 3,
            Description: "Full path to the local file location")]
        public string LocalFile { get; set; }

        #endregion

        public DownloadFile()
        {
            // Default values
            RemoteFile = "/tmp/img100.jpg";
            LocalFile = @"C:\Temp\img100.jpg";

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

        public override void Run()
        {
            try
            {
                var files = new List<(string, string)>
                {
                    (RemoteFile, LocalFile)
                };

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