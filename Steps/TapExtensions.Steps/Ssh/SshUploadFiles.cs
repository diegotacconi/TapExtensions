using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Ssh
{
    [Display("SshUploadFiles",
        Groups: new[] { "TapExtensions", "Steps", "Ssh" })]
    public class SshUploadFiles : TestStep
    {
        #region Settings

        [Display("Dut", Order: 1)] public ISecureShell Dut { get; set; }

        public class LocalAndRemoteFilePair : ValidatingObject
        {
            [Display("Local Filename", Order: 1, Description: "Full path to the local file location")]
            [FilePath]
            public string LocalFilename
            {
                get => _localFilename;
                set => _localFilename = !string.IsNullOrWhiteSpace(value) ? Path.GetFullPath(value) : "";
            }

            private string _localFilename;

            [Display("Remote Filename", Order: 2, Description: "Full path to the remote file location")]
            public string RemoteFilename { get; set; }

            public LocalAndRemoteFilePair()
            {
                // Validation rules
                Rules.Add(() => !string.IsNullOrWhiteSpace(LocalFilename),
                    "Filename cannot be empty", nameof(LocalFilename));
                Rules.Add(() => !string.IsNullOrWhiteSpace(RemoteFilename),
                    "Filename cannot be empty", nameof(RemoteFilename));
                Rules.Add(() => File.Exists(LocalFilename),
                    "File not found", nameof(LocalFilename));
            }
        }

        [Display("Files", Order: 5)] public List<LocalAndRemoteFilePair> Files { get; set; }

        #endregion

        public SshUploadFiles()
        {
            // Default values
            Files = new List<LocalAndRemoteFilePair>
            {
                new LocalAndRemoteFilePair
                {
                    LocalFilename = @"C:\Windows\Web\Screen\img103.png",
                    RemoteFilename = "/tmp/img103.png"
                }
            };
        }

        public override void Run()
        {
            try
            {
                var files = Files.Select(file => (file.LocalFilename, file.RemoteFilename)).ToList();
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