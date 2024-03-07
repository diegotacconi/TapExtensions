using System;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Ssh
{
    [Display("SshDisconnect",
        Groups: new[] { "TapExtensions", "Steps", "Dut" })]
    public class SshDisconnect : TestStep
    {
        [Display("Dut", Order: 1)] public ISsh Dut { get; set; }

        public override void Run()
        {
            try
            {
                Dut.Disconnect();

                if (Dut.IsConnected)
                    throw new InvalidOperationException(
                        $"Cannot disconnect from '{Dut.Name}'");

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