using System;
using OpenTap;
using TapExtensions.Interfaces.Uart;

namespace TapExtensions.Steps.Uart
{
    [Display("UartQuery",
        Groups: new[] { "TapExtensions", "Steps", "Uart" })]
    public class UartQuery : TestStep
    {
        #region Settings

        [Display("Uart Dut", Order: 1)] public IUart Dut { get; set; }

        [Display("Command", Order: 2)] public string Command { get; set; }

        [Display("Expected Response", Order: 3)]
        public string ExpectedResponse { get; set; }

        [Display("Expected EndOfMessage", Order: 4, Description: "Usually a shell prompt")]
        public string ExpectedEndOfMessage { get; set; }

        [Display("Timeout", Order: 5)]
        [Unit("s")]
        public int Timeout { get; set; }

        #endregion

        public UartQuery()
        {
            // Default values
            Command = "pwd";
            ExpectedResponse = "/home/user";
            ExpectedEndOfMessage = "user@hostname#";
            Timeout = 5;

            // Validation rules
            Rules.Add(() => Timeout > 0,
                "Timeout must be greater than zero", nameof(Timeout));
        }

        public override void Run()
        {
            try
            {
                var response = Dut.Query(Command, ExpectedEndOfMessage, Timeout);

                if (!response.Contains(ExpectedResponse))
                    throw new InvalidOperationException(
                        $"Cannot find '{ExpectedResponse}' in the response to the ssh command of '{Command}'");

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