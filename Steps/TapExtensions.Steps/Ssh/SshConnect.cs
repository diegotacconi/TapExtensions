using System;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Ssh
{
    [Display("SshConnect",
        Groups: new[] { "TapExtensions", "Steps", "Ssh" })]
    [AllowAnyChild]
    public class SshConnect : TestStep
    {
        [Display("Dut", Order: 1)] public ISsh Dut { get; set; }

        public override void Run()
        {
            if (Dut.IsConnected)
            {
                Log.Debug($"'{Dut.Name}' already connected");
                return;
            }

            try
            {
                Dut.Connect();

                if (!Dut.IsConnected)
                    throw new InvalidOperationException(
                        $"Cannot connect to '{Dut.Name}'");

                UpgradeVerdict(Verdict.Pass);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }

            // RunChildSteps();
        }
    }
}