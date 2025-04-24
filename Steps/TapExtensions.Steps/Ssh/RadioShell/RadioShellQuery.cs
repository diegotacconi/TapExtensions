using System;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Ssh.RadioShell
{
    [Display("RadioShellQuery",
        Groups: new[] { "TapExtensions", "Steps", "Ssh", "RadioShell" })]
    public class RadioShellQuery : TestStep
    {
        #region Settings

        [Display("Dut", Order: 1)] public IRadioShell Dut { get; set; }

        [Display("Command", Order: 2)] public string Command { get; set; }

        #endregion

        public RadioShellQuery()
        {
            // Default values
            Command = "";

            // Validation rules
            Rules.Add(() => !string.IsNullOrEmpty(Command),
                "Command cannot be empty", nameof(Command));
        }

        public override void Run()
        {
            if (!Dut.IsConnected)
                throw new InvalidOperationException(
                    "Not connected to DUT");

            try
            {
                Dut.SendRadioCommand(Command, out _);
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