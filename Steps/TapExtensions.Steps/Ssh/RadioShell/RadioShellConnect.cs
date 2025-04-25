using System;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Ssh.RadioShell
{
    [Display("RadioShellConnect",
        Groups: new[] { "TapExtensions", "Steps", "Ssh", "RadioShell" },
        Description: "Use SSH to communicate with DUT or optionally open a connection to the internal Radio shell.")]
    public class RadioShellConnect : TestStep
    {
        #region Settings

        [Display("Dut", Order: 1)] public ISecureShell Dut { get; set; }

        [Display("Ping Timeout", Order: 2)]
        [Unit("s")]
        public uint PingTimeout { get; set; } = 30;

        [Display("Min Ping Replies", Order: 3,
            Description: "Minimum number of successful consecutive ping replies")]
        [Unit("Pings")]
        public uint MinSuccessfulPingReplies { get; set; } = 4;

        #endregion

        public override void Run()
        {
            if (Dut.IsConnected)
            {
                Log.Debug($"'{Dut.Name}' already connected");
                UpgradeVerdict(Verdict.Pass);
                return;
            }

            try
            {
                var pingTimeoutMs = PingTimeout * 1000;
                const uint pingRetryIntervalMs = 2000;

                if (Dut.Ping(pingTimeoutMs, pingRetryIntervalMs, MinSuccessfulPingReplies))
                {
                    Dut.Connect();
                    if (Dut is IRadioShell radioShell)
                        radioShell.ConnectDutRadio(pingTimeoutMs, 0);
                }

                if (!Dut.IsConnected)
                    throw new InvalidOperationException(
                        $"Cannot connect to '{Dut.Name}'");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                Dut.Disconnect();
            }

            UpgradeVerdict(Dut.IsConnected ? Verdict.Pass : Verdict.Fail);
        }
    }
}