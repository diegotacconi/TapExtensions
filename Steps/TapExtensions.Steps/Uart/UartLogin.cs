using System;
using System.Diagnostics;
using OpenTap;
using TapExtensions.Interfaces.Uart;

namespace TapExtensions.Steps.Uart
{
    [Display("UartLogin",
        Groups: new[] { "TapExtensions", "Steps", "Uart" })]
    public class UartLogin : TestStep
    {
        #region Settings

        [Display("Uart Dut", Order: 1)] public IUart Dut { get; set; }

        [Display("Expected Login Message", Order: 2)]
        public string ExpectedLoginPrompt { get; set; }

        [Display("Username", Order: 3)] public string Username { get; set; }

        [Display("Expected Password Message", Order: 4)]
        public string ExpectedPasswordPrompt { get; set; }

        [Display("Password", Order: 4)] public string Password { get; set; }

        [Display("Expected Shell Prompt", Order: 5)]
        public string ExpectedShellPrompt { get; set; }

        [Display("Command Timeout", Order: 6,
            Description: "Timeout when waiting for the response to a single command")]
        [Unit("s")]
        public int CommandTimeout { get; set; }

        [Display("Retry Period", Order: 7,
            Description: "Time period to retry checking for the login prompt")]
        [Unit("s")]
        public double RetryPeriod { get; set; }

        [Display("Retry Timeout", Order: 8,
            Description: "Timeout when retrying for the login prompt")]
        [Unit("s")]
        public int RetryTimeout { get; set; }

        #endregion

        public UartLogin()
        {
            // Default values
            ExpectedLoginPrompt = "login:";
            Username = "pi";
            ExpectedPasswordPrompt = "Password:";
            Password = "raspberry";
            ExpectedShellPrompt = "$";
            CommandTimeout = 20;
            RetryPeriod = 5;
            RetryTimeout = 60;

            // Validation rules
            Rules.Add(() => CommandTimeout > 0,
                "Must be greater than zero", nameof(CommandTimeout));
            Rules.Add(() => RetryPeriod >= 0,
                "Must be greater than or equal to zero", nameof(RetryPeriod));
            Rules.Add(() => RetryTimeout > 0,
                "Must be greater than zero", nameof(RetryTimeout));
        }

        public override void Run()
        {
            try
            {
                // Try to get the login prompt
                var loginIsReady = CheckForLogin(RetryTimeout, RetryPeriod);
                if (!loginIsReady)
                    throw new InvalidOperationException(
                        $"Unable to find for Login prompt of '{ExpectedLoginPrompt}'");

                // Enter username
                Dut.Query(Username, ExpectedPasswordPrompt, CommandTimeout);

                // Enter password
                Dut.Query(Password, ExpectedShellPrompt, CommandTimeout);

                UpgradeVerdict(Verdict.Pass);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }

        private bool CheckForLogin(double timeout, double interval)
        {
            var loginIsReady = false;
            var keepChecking = true;
            var timer = new Stopwatch();
            timer.Start();

            do
            {
                try
                {
                    // Check for login prompt
                    var response = Dut.Query("\r", ExpectedLoginPrompt, CommandTimeout);
                    if (response.Contains(ExpectedLoginPrompt))
                    {
                        loginIsReady = true;
                        keepChecking = false;
                    }
                }
                catch (Exception ex)
                {
                    // Ignore exceptions
                    if (!string.IsNullOrWhiteSpace(ex.Message))
                        Log.Warning(ex.Message);
                }

                if (timer.Elapsed > TimeSpan.FromSeconds(timeout))
                {
                    Log.Warning($"Timeout occurred at {timeout} s");
                    keepChecking = false;
                }

                if (keepChecking)
                    TapThread.Sleep(TimeSpan.FromSeconds(interval));

                OfferBreak();
            } while (keepChecking);

            return loginIsReady;
        }
    }
}