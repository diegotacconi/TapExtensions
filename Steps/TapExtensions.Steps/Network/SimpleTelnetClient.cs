using System;
using System.Net;
using System.Net.Sockets;
using OpenTap;

namespace TapExtensions.Steps.Network
{
    [Display("SimpleTelnetClient",
        Groups: new[] { "TapExtensions", "Steps", "Network" })]
    public class SimpleTelnetClient : TestStep
    {
        #region Settings

        [Display("IP Address", Order: 1)] public string IpAddress { get; set; }

        [Display("Port", Order: 2)] public int TcpPort { get; set; }

        [Display("Timeout", Order: 3)]
        [Unit("s")]
        public int Timeout { get; set; }

        #endregion

        private TcpClient _tcpClient;
        private NetworkStream _tcpStream;
        private bool IsOpen => _tcpClient != null && _tcpClient.Connected;

        public SimpleTelnetClient()
        {
            // Default values
            IpAddress = "192.168.4.100";
            TcpPort = 3000;
            Timeout = 20;

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
                Connect();
                try
                {
                    // Monitor
                    TapThread.Sleep(TimeSpan.FromSeconds(Timeout));
                }
                finally
                {
                    Disconnect();
                }

                UpgradeVerdict(Verdict.Pass);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }

        private void Connect()
        {
            if (IsOpen)
                return;

            Log.Debug($"Connecting to {IpAddress}:{TcpPort}");
            _tcpClient = new TcpClient(IpAddress, TcpPort);
            _tcpStream = _tcpClient.GetStream();
            _tcpStream.ReadTimeout = 5000;
            _tcpStream.WriteTimeout = 5000;
        }

        private void Disconnect()
        {
            if (!IsOpen)
                return;

            Log.Debug($"Disconnecting from {IpAddress}:{TcpPort}");
            _tcpStream?.Close();
            _tcpClient?.Close();
        }
    }
}