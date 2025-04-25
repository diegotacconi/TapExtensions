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

        [Display("Radio Command", Order: 2)] public string RadioCommand { get; set; }

        #endregion

        public RadioShellQuery()
        {
            // Default values
            RadioCommand = "";

            // Validation rules
            Rules.Add(() => !string.IsNullOrEmpty(RadioCommand),
                "Command cannot be empty", nameof(RadioCommand));
        }

        public override void Run()
        {
            if (!Dut.IsConnected)
                throw new InvalidOperationException(
                    "Not connected to DUT");

            try
            {
                Dut.SendRadioCommand(RadioCommand, out _);
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