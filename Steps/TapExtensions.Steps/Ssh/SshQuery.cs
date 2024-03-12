using System;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Ssh
{
    [Display("SshQuery",
        Groups: new[] { "TapExtensions", "Steps", "Ssh" })]
    public class SshQuery : TestStep
    {
        #region Settings

        [Display("Dut", Order: 1)] public ISsh Dut { get; set; }

        [Display("Command", Order: 2)] public string Command { get; set; }

        [Display("Expected Response", Order: 3)]
        public string ExpectedResponse { get; set; }

        [Display("Timeout", Order: 4)]
        [Unit("s")]
        public int Timeout { get; set; }

        #endregion

        public SshQuery()
        {
            // Default values
            Command = "pwd";
            ExpectedResponse = "/";
            Timeout = 5;

            // Validation rules
            Rules.Add(() => Timeout >= 0,
                "Must be greater than or equal to zero", nameof(Timeout));
        }

        public override void Run()
        {
            try
            {
                if (!Dut.Query(Command, Timeout, out var response))
                    throw new InvalidOperationException(
                        "Exit status of ssh command was not 0");

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