using System;
using System.Collections.Generic;
using System.IO;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Ssh
{
    [Display("SshUploadFile",
        Groups: new[] { "TapExtensions", "Steps", "Ssh" })]
    public class SshUploadFile : TestStep
    {
        #region Settings

        [Display("Dut", Order: 1)] public ISecureShell Dut { get; set; }

        [Display("Local File (Source)", Order: 2,
            Description: "Full path of the local file location")]
        [FilePath]
        public string LocalFile
        {
            get => _localFile;
            set => _localFile = !string.IsNullOrWhiteSpace(value) ? Path.GetFullPath(value) : "";
        }

        private string _localFile;

        [Display("Remote File (Destination)", Order: 3,
            Description: "Full path to the remote file location")]
        public string RemoteFile { get; set; }

        #endregion

        public SshUploadFile()
        {
            // Default values
            LocalFile = @"C:\Windows\Web\Screen\img100.jpg";
            RemoteFile = "/tmp/img100.jpg";

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

        public override void Run()
        {
            try
            {
                var files = new List<(string, string)>
                {
                    (LocalFile, RemoteFile)
                };

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