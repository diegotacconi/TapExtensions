using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Network
{
    [Display("PingDut",
        Groups: new[] { "TapExtensions", "Steps", "Network" })]
    public class PingDut : BasePing
    {
        [Display("Dut", Order: 1)] public ISecureShell Dut { get; set; }

        public override void Run()
        {
            BaseRun(Dut.IpAddress);
        }
    }

    [Display("Ping",
        Groups: new[] { "TapExtensions", "Steps", "Network" })]
    public class Ping : BasePing
    {
        [Display("IP Address", Order: 1)] public string IpAddress { get; set; }

        public Ping()
        {
            // Default values
            IpAddress = "127.0.0.1";

            // Validation rules
            Rules.Add(() => IPAddress.TryParse(IpAddress, out _),
                "IP Address is not a valid", nameof(IpAddress));
        }

        public override void Run()
        {
            BaseRun(IpAddress);
        }
    }

    public abstract class BasePing : TestStep
    {
        [Display("Min Ping Replies", Order: 2,
            Description: "Minimum number of successful ping replies required for passing")]
        [Unit("Pings")]
        public int MinSuccessfulReplies { get; set; }

        [Display("Timeout", Order: 3)]
        [Unit("s")]
        public int Timeout { get; set; }

        private protected BasePing()
        {
            // Default values
            MinSuccessfulReplies = 4;
            Timeout = 30;

            // Validation rules
            Rules.Add(() => MinSuccessfulReplies >= 0,
                "Minimum number of successful replies " +
                "must be greater than or equal to zero", nameof(MinSuccessfulReplies));
            Rules.Add(() => Timeout > 0,
                "Timeout must be greater than zero", nameof(Timeout));
        }

        private protected void BaseRun(string ipAddress)
        {
            ThrowOnValidationError(true);
            try
            {
                var pingOk = PingHost(ipAddress, Timeout, MinSuccessfulReplies);
                UpgradeVerdict(pingOk ? Verdict.Pass : Verdict.Fail);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }

        private protected bool PingHost(string ipAddress, int timeout, int minSuccessfulReplies,
            double delayBetweenPings = 1.0)
        {
            var pingOk = false;
            var pingOkReplies = 0;
            var timer = new Stopwatch();
            var timeoutMs = (long)timeout * 1000;

            using (var pingSender = new System.Net.NetworkInformation.Ping())
            {
                // Create a buffer of 32 bytes of data to be transmitted.
                var buffer = Encoding.ASCII.GetBytes("12345678901234567890123456789012");

                // Check for valid IP Address
                if (!IPAddress.TryParse(ipAddress, out var address))
                    throw new InvalidOperationException("IP Address is not a valid");

                Log.Info($"Pinging {address}");
                timer.Start();

                while (timer.ElapsedMilliseconds < timeoutMs)
                {
                    // Use same timeout as in DOS prompt default, which is 4 seconds
                    var reply = pingSender.Send(address, 4000, buffer);
                    if (reply != null && reply.Status == IPStatus.Success)
                    {
                        // Ping success
                        var roundtripTime = reply.RoundtripTime < 1 ? "<1ms" : $"={reply.RoundtripTime}ms";
                        Log.Debug(
                            $"Ping reply from {reply.Address}: bytes={reply.Buffer.Length} time{roundtripTime} TTL={reply.Options.Ttl}");

                        pingOkReplies++;
                        if (pingOkReplies >= minSuccessfulReplies)
                        {
                            pingOk = true;
                            break;
                        }
                    }
                    else
                    {
                        // Ping failure
                        if (reply != null)
                        {
                            // Convert camelCase to sentence with spaces
                            var status = Regex.Replace(reply.Status.ToString(), "([A-Z0-9]+)", " $1").ToLower().Trim();
                            Log.Debug($"Ping request {status} (ping failed).");
                        }
                        else
                        {
                            Log.Debug("Ping request failed.");
                        }

                        pingOkReplies = 0;
                    }

                    TapThread.Sleep(TimeSpan.FromSeconds(delayBetweenPings));
                    OfferBreak();
                }
            }

            return pingOk;
        }
    }
}