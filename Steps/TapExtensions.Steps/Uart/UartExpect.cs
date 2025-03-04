using System;
using OpenTap;
using TapExtensions.Interfaces.Uart;

namespace TapExtensions.Steps.Uart
{
    [Display("UartExpect",
        Groups: new[] { "TapExtensions", "Steps", "Uart" })]
    public class UartExpect : TestStep
    {
        #region Settings

        [Display("Uart Dut", Order: 1)] public IUart Dut { get; set; }

        [Display("Expected Response", Order: 2)]
        public string ExpectedResponse { get; set; }

        [Display("Timeout", Order: 3)]
        [Unit("s")]
        public int Timeout { get; set; }

        #endregion

        public UartExpect()
        {
            // Default values
            ExpectedResponse = "login:";
            Timeout = 80;

            // Validation rules
            Rules.Add(() => Timeout > 0,
                "Timeout must be greater than zero", nameof(Timeout));
        }

        public override void Run()
        {
            try
            {
                var success = Dut.Expect(ExpectedResponse, Timeout);
                UpgradeVerdict(success ? Verdict.Pass : Verdict.Fail);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }
    }
}