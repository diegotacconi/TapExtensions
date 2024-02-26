using System;
using OpenTap;
using TapExtensions.Interfaces.Duts;

namespace TapExtensions.Steps.Dut
{
    [Display("SshQuery",
        Groups: new[] { "TapExtensions", "Steps", "Dut" })]
    public class SshQuery : TestStep
    {
        #region Settings

        [Display("Dut", Order: 1)]
        public IDutControlSsh Dut { get; set; }

        [Display("Command", Order: 2)]
        public string Command { get; set; }

        [Display("ExpectedResponse", Order: 3)]
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
        }

        public override void Run()
        {
            if (!Dut.IsConnected)
                throw new InvalidOperationException(
                    "Dut is not connected");

            try
            {
                var timeoutMs = Timeout * 1000;
                var response = Dut.SendSshQuery(Command, timeoutMs);
                if (!response.Contains(ExpectedResponse))
                    throw new InvalidOperationException(
                        $"Cannot find '{ExpectedResponse}' in the response to the ssh command of '{Command}'");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
    }
}