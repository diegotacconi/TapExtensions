using System;
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

        #endregion

        public override void Run()
        {
            throw new NotImplementedException();
        }
    }
}