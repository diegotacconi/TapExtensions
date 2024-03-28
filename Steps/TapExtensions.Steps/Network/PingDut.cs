﻿using System;
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
    public class PingDut : TestStep
    {
        #region Settings

        [Display("Dut", Order: 1)] public ISecureShell Dut { get; set; }

        [Display("Min Ping Replies", Order: 2,
            Description: "Minimum number of successful ping replies required for this test to pass")]
        [Unit("Pings")]
        public int MinSuccessfulReplies { get; set; }

        [Display("Timeout", Order: 3)]
        [Unit("s")]
        public int Timeout { get; set; }

        #endregion

        public PingDut()
        {
            // Default values
            MinSuccessfulReplies = 4;
            Timeout = 30;

            // Validation rules
            Rules.Add(() => Timeout > 0,
                "Timeout must be greater than zero", nameof(Timeout));
        }

        public override void Run()
        {
            try
            {
                var pingOk = PingHost(Dut.IpAddress, Timeout, MinSuccessfulReplies);
                UpgradeVerdict(pingOk ? Verdict.Pass : Verdict.Fail);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }

        private bool PingHost(string ipAddress, int timeout, int minSuccessfulReplies)
        {
            var pingOk = false;
            var pingOkReplies = 0;
            var timer = new Stopwatch();
            var timeoutMs = (long)timeout * 1000;

            using (var pingSender = new System.Net.NetworkInformation.Ping())
            {
                // Create a buffer of 32 bytes of data to be transmitted.
                var buffer = Encoding.ASCII.GetBytes("12345678901234567890123456789012");

                var address = IPAddress.Parse(ipAddress);
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

                    TapThread.Sleep(TimeSpan.FromSeconds(1));
                    OfferBreak();
                }
            }

            return pingOk;
        }
    }
}