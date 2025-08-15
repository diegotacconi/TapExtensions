using System;
using OpenTap;
using TapExtensions.Interfaces.Uart;

namespace TapExtensions.Steps.Uart
{
    [Display("UartLogin",
        Groups: new[] { "TapExtensions", "Steps", "Uart" })]
    public class UartLogin : TestStep
    {
        #region Settings

        [Display("Uart Dut", Order: 1)] public IUartDut UartDut { get; set; }

        [Display("Expected Username Prompt", Order: 2)]
        public string ExpectedUsernamePrompt { get; set; }

        [Display("Expected Password Prompt", Order: 3)]
        public string ExpectedPasswordPrompt { get; set; }

        [Display("Expected Shell Prompt", Order: 4)]
        public string ExpectedShellPrompt { get; set; }

        [Display("Timeout", Order: 5)]
        [Unit("s")]
        public int Timeout { get; set; }

        #endregion

        public UartLogin()
        {
            // Default values
            ExpectedUsernamePrompt = "login:";
            ExpectedPasswordPrompt = "Password:";
            ExpectedShellPrompt = "$";
            Timeout = 10;

            // Validation rules
            Rules.Add(() => Timeout > 0,
                "Must be greater than zero", nameof(Timeout));
        }

        public override void Run()
        {
            try
            {
                UartDut.Login(ExpectedUsernamePrompt, ExpectedPasswordPrompt, ExpectedShellPrompt, Timeout);
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