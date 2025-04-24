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

        [Display("Timeout", Order: 2,
            Description: "Timeout  value in milliseconds. Defines how long we wait for successful connection.")]
        [Unit("ms")]
        public uint Timeout { get; set; } = 30000;

        [Display("Min Ping Replies", Order: 3,
            Description: "Minimum number of successful ping replies required for passing")]
        [Unit("Pings")]
        public uint MinSuccessfulPingReplies { get; set; } = 4;

        [Display("Ping interval", Order: 4,
            Description: "Interval how quick another ping request is sent.")]
        [Unit("ms")]
        public uint PingInterval { get; set; } = 2000;

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
                if (Dut.Ping(Timeout, PingInterval, MinSuccessfulPingReplies))
                {
                    Dut.Connect();
                    if (Dut is IRadioShell radioShell)
                        radioShell.ConnectDutRadio(Timeout, 0);
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