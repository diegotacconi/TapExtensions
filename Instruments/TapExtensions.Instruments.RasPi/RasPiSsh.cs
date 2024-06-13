using System;
using System.Net;
using OpenTap;
using Renci.SshNet;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Instruments.RasPi
{
    [Display("RasPiSsh",
        Groups: new[] { "TapExtensions", "Instruments", "RasPi" })]
    public class RasPiSsh : Instrument, IGpio
    {
        #region Settings

        [Display("IP Address", Order: 1, Group: "SSH Settings")]
        public string IpAddress { get; set; }

        [Display("Port", Order: 2, Group: "SSH Settings", Description: "TCP port number (default value is 22)")]
        public int TcpPort { get; set; }

        [Display("Username", Order: 3, Group: "SSH Settings")]
        public string Username { get; set; }

        [Display("Password", Order: 4, Group: "SSH Settings")]
        public string Password { get; set; }

        [Display("Keep Alive Interval", Order: 5, Group: "SSH Settings")]
        [Unit("s")]
        public int KeepAliveInterval { get; set; }

        [Display("Verbose Logging", Order: 6, Group: "Debug", Collapsed: true,
            Description: "Enables verbose logging of SSH communication.")]
        public bool VerboseLoggingEnabled { get; set; } = true;

        #endregion

        private SshClient _sshClient;

        public RasPiSsh()
        {
            // Default values
            Name = nameof(RasPiSsh);
            IpAddress = "192.168.100.1";
            TcpPort = 22;
            Username = "pi";
            Password = "";
            KeepAliveInterval = 30;

            // Validation rules
            Rules.Add(() => IPAddress.TryParse(IpAddress, out _),
                "Not a valid IPv4 Address", nameof(IpAddress));
            Rules.Add(() => KeepAliveInterval >= 0,
                "Must be greater than or equal to zero", nameof(KeepAliveInterval));
        }

        public override void Open()
        {
            base.Open();
            IsConnected = false;
        }

        public override void Close()
        {
            SshDisconnect();
            base.Close();
            IsConnected = false;
        }

        private void SshConnect()
        {
            if (_sshClient == null)
                _sshClient = new SshClient(IpAddress, TcpPort, Username, Password)
                    { KeepAliveInterval = TimeSpan.FromSeconds(KeepAliveInterval) };

            if (!_sshClient.IsConnected)
            {
                if (VerboseLoggingEnabled)
                    Log.Debug($"Connecting to {IpAddress} on port {TcpPort}");

                _sshClient.Connect();
                IsConnected = true;
            }
        }

        private void SshDisconnect()
        {
            if (_sshClient == null)
                return;

            if (VerboseLoggingEnabled)
                Log.Debug($"Disconnecting from {IpAddress}");

            _sshClient.Disconnect();
            _sshClient.Dispose();
            _sshClient = null;
            IsConnected = false;
        }

        public void SetPinState(int pin, EPinState state)
        {
            SshConnect();
            try
            {
                // ToDo:
                /*
                    /sys/class/gpio/gpio11/direction
                    /sys/class/gpio/gpio11/value
                    /dev/gpiochipN
                    sudo usermod -a -G gpio <username>
                */
            }
            finally
            {
                SshDisconnect();
            }
        }

        public EPinState GetPinState(int pin)
        {
            throw new NotImplementedException();
        }
    }
}