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

        [Display("Uart Dut", Order: 1)]
        public IUart Uart { get; set; }

        [Display("Command", Order: 2)]
        public string Command { get; set; }

        [Display("Expected Response", Order: 3)]
        public string ExpectedResponse { get; set; }

        [Display("Expected Prompt", Order: 4)]
        public string ExpectedPrompt { get; set; }

        [Display("Timeout", Order: 5)]
        [Unit("s")]
        public int Timeout { get; set; }

        #endregion

        public UartQuery()
        {
            // Default values
            Command = "pwd";
            ExpectedResponse = "/home/user";
            ExpectedPrompt = "user@hostname#";
            Timeout = 5;

            // Validation rules
            Rules.Add(() => Timeout > 0,
                "Timeout must be greater than zero", nameof(Timeout));
        }

        public override void Run()
        {
            try
            {
                var response = Uart.Query(Command, ExpectedPrompt, Timeout);
                // Publish(Name, response.Contains(ExpectedResponse), true, true, "bool");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                // Publish(Name, false, true, true, "bool");
            }
        }
    }
}