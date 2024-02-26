using System;
using OpenTap;
using TapExtensions.Interfaces.Duts;

namespace TapExtensions.Steps.Dut
{
    [Display("SshConnect",
        Groups: new[] { "TapExtensions", "Steps", "Dut" })]
    [AllowAnyChild]
    public class SshConnect : TestStep
    {
        #region Settings

        [Display("Dut", Order: 1)]
        public IDutControlSsh Dut { get; set; }

        #endregion

        public override void Run()
        {
            if (Dut.IsConnected)
            {
                Log.Debug($"'{Dut.Name}' already connected");
                return;
            }

            try
            {
                Dut.ConnectDut();

                if (Dut.IsConnected)
                    Log.Debug($"'{Dut.Name}' connected");
                else
                    throw new InvalidOperationException(
                        $"Could not connect to '{Dut.Name}'");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                // Dut.DisconnectDut();
            }
        }
    }
}