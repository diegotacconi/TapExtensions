using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using OpenTap;

namespace TapExtensions.Steps.Network
{
    [Display("Ping",
        Groups: new[] {"TapExtensions", "Steps", "Network"})]
    public class Ping : TestStep
    {
        #region Settings

        [Display("IP Address", Order: 1)]
        public string IpAddress { get; set; }

        [Display("Min Ping Replies", Order: 2, Description: "Minimum number of successful ping replies required for this test to pass")]
        [Unit("Pings")]
        public int MinSuccessfulReplies { get; set; }

        [Display("Timeout", Order: 3)]
        [Unit("s")]
        public int Timeout { get; set; }

        #endregion

        public Ping()
        {
            // Default values
            IpAddress = "127.0.0.1";
            MinSuccessfulReplies = 4;
            Timeout = 30;

            // Validation rules
            Rules.Add(() => IPAddress.TryParse(IpAddress, out _),
                "Not a valid IPv4 Address", nameof(IpAddress));
            Rules.Add(() => Timeout > 0,
                "Timeout must be greater than zero", nameof(Timeout));
        }

        public override void Run()
        {
            try
            {
                var ipAddress = IPAddress.Parse(IpAddress);
                var timeoutMs = Timeout * 1000;
                var pingOk = PingHost(ipAddress, timeoutMs, MinSuccessfulReplies);

                // Publish(Name, pingOk, true, true, "bool");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                // Publish(Name, false, true, true, "bool");
            }
        }

        private bool PingHost(IPAddress ipAddress, int timeoutMs, int minSuccessfulReplies)
        {
            var keepOnPinging = true;
            var pingOk = false;
            var pingOkReplies = 0;
            var timer = new Stopwatch();

            using (var pingSender = new System.Net.NetworkInformation.Ping())
            {
                // Create a buffer of 32 bytes of data to be transmitted.
                var buffer = Encoding.ASCII.GetBytes("12345678901234567890123456789012");

                Log.Info($"Pinging {ipAddress}");
                timer.Start();
                do
                {
                    // Use same timeout as in DOS prompt default, which is 4 seconds
                    var reply = pingSender.Send(ipAddress, 4000, buffer);
                    if (reply != null && reply.Status == IPStatus.Success)
                    {
                        // Ping success
                        var roundtripTime = reply.RoundtripTime < 1 ? "<1ms" : string.Format($"={reply.RoundtripTime}ms");
                        Log.Debug(
                            $"Ping reply from {reply.Address}: bytes={reply.Buffer.Length} time{roundtripTime} TTL={reply.Options.Ttl}");

                        pingOkReplies++;
                        if (pingOkReplies >= minSuccessfulReplies)
                        {
                            keepOnPinging = false;
                            pingOk = true;
                        }
                        else
                        {
                            TapThread.Sleep(1000);
                        }
                    }
                    else
                    {
                        // Ping failure
                        if (reply != null)
                        {
                            // Convert camelCase to sentence with spaces
                            var status = Regex.Replace(reply.Status.ToString(), "([A-Z0-9]+)", " $1").ToLower().Trim();
                            Log.Warning($"Ping request {status}.");
                        }
                        else
                        {
                            Log.Warning("Ping request failed.");
                        }

                        if (timer.ElapsedMilliseconds > timeoutMs)
                            keepOnPinging = false;

                        pingOkReplies = 0;
                    }

                    // Offer GUI to break at this point
                    OfferBreak();

                } while (keepOnPinging);
            }

            return pingOk;
        }
    }
}