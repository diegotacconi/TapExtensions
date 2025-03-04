using System;
using OpenTap;
using TapExtensions.Interfaces.Uart;

namespace TapExtensions.Steps.Uart
{
    [Display("UartMonitor",
        Groups: new[] { "TapExtensions", "Steps", "Uart" })]
    public class UartMonitor : TestStep
    {
        [Display("Uart Dut", Order: 1)] public IUart Dut { get; set; }

        public override void Run()
        {
            try
            {
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