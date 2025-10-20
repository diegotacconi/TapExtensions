using System;
using System.IO;
using OpenTap;

namespace TapExtensions.Steps.Process
{
    [Display("CreateDirectory",
        Groups: new[] { "TapExtensions", "Steps", "Process" })]
    public class CreateDirectory : TestStep
    {
        [Display("DirectoryPath", Order: 1,
            Description: "The name of the directory to be created.")]
        [DirectoryPath]
        public string DirectoryPath { get; set; } = @"C:\Temp\Example\";

        public override void Run()
        {
            try
            {
                if (!Directory.Exists(DirectoryPath))
                {
                    Directory.CreateDirectory(DirectoryPath);
                    Log.Debug($"Directory '{DirectoryPath}' created");
                }
                else
                {
                    Log.Debug($"Directory '{DirectoryPath}' already exists");
                }

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