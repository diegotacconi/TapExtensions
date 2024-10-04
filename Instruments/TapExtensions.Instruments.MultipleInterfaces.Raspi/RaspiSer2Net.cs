// https://github.com/cminyard/ser2net

using System;
using System.Net;
using System.Text;
using OpenTap;
using TapExtensions.Interfaces.Uart;
using TapExtensions.Shared.Telnet;

namespace TapExtensions.Instruments.MultipleInterfaces.Raspi
{
    [Display("RaspiSer2Net",
        Groups: new[] { "TapExtensions", "Instruments", "MultipleInterfaces" })]
    public class RaspiSer2Net : Resource, IUart
    {
        #region Settings

        [Display("IP Address", Order: 1)] public string IpAddress { get; set; }

        [Display("Tcp Port", Order: 2)] public int TcpPort { get; set; }

        #endregion

        private static TelnetClient _client;

        public RaspiSer2Net()
        {
            // Default values
            Name = nameof(RaspiSer2Net);
            IpAddress = "192.168.4.100";
            TcpPort = 3000;

            // Validation rules
            Rules.Add(() => IPAddress.TryParse(IpAddress, out _),
                "Not a valid IPv4 Address", nameof(IpAddress));
        }

        public override void Open()
        {
            base.Open();
            Connect();
            IsConnected = false;
        }

        public override void Close()
        {
            Disconnect();
            IsConnected = false;
            base.Close();
        }

        private void Connect()
        {
            _client = new TelnetClient(IpAddress, TcpPort);
            _client.Events.Connected += Connected;
            _client.Events.Disconnected += Disconnected;
            _client.Events.DataReceived += DataReceived;
            _client.Events.DataSent += DataSent;
            _client.Settings.ConnectTimeoutMs = 5000;
            _client.Settings.NoDelay = true;
            _client.Settings.IdleServerTimeoutMs = 10000;
            _client.Logger = Logger;

            _client.Connect();
        }

        private void Disconnect()
        {
            _client.Disconnect();
        }

        private void Connected(object sender, ConnectionEventArgs e)
        {
            Log.Debug("*** Server " + e.IpPort + " connected");
        }

        private void Disconnected(object sender, ConnectionEventArgs e)
        {
            Log.Debug("*** Server " + e.IpPort + " disconnected");
        }

        private void DataReceived(object sender, DataReceivedEventArgs e)
        {
            Log.Debug("[" + e.IpPort + "] " + Encoding.UTF8.GetString(e.Data.Array, 0, e.Data.Count));
        }

        private void DataSent(object sender, DataSentEventArgs e)
        {
            Log.Debug("[" + e.IpPort + "] sent " + e.BytesSent + " bytes");
        }

        private void Logger(string msg)
        {
            Log.Debug(msg);
        }

        public bool Expect(string expectedResponse, int timeout)
        {
            throw new NotImplementedException();
        }

        public string Query(string command, string expectedEndOfMessage, int timeout)
        {
            throw new NotImplementedException();
        }

        public void Write(string command)
        {
            throw new NotImplementedException();
        }
    }
}