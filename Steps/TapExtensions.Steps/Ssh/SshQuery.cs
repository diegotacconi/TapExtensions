using System;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Ssh
{
    [Display("SshQuery",
        Groups: new[] {"TapExtensions", "Steps", "Ssh"})]
    public class SshQuery : TestStep
    {
        #region Settings

        [Display("Ssh Dut", Order: 1)]
        public ISsh Ssh { get; set; }

        [Display("Command", Order: 2)]
        public string Command { get; set; }

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
        }

        public override void Run()
        {
            try
            {
                Ssh.Connect();

                var response = Ssh.Query(Command, Timeout);
                if (!response.Contains(ExpectedResponse))
                    throw new InvalidOperationException(
                        $"Cannot find '{ExpectedResponse}' in the response to the ssh command of '{Command}'");

                Ssh.Disconnect();
                // Publish(Name, true, true, true, "bool");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                // Publish(Name, false, true, true, "bool");
            }
        }
    }
}