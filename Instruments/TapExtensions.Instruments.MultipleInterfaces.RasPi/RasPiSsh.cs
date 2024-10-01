using System;
using System.Collections.Generic;
using System.Net;
using OpenTap;
using Renci.SshNet;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Instruments.MultipleInterfaces.Raspi
{
    [Display("RaspiSsh",
        Groups: new[] { "TapExtensions", "Instruments", "MultipleInterfaces" })]
    public class RaspiSsh : Resource, ISecureShell
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

        [Display("Use SSH Tunnel", Order: 10, Groups: new[] { "SSH Settings", "SSH Tunnel" }, Collapsed: true)]
        public bool UseSshTunnel { get; set; }

        [EnabledIf(nameof(UseSshTunnel), HideIfDisabled = true)]
        [Display("Bound Host", Order: 11, Groups: new[] { "SSH Settings", "SSH Tunnel" }, Collapsed: true,
            Description: "local computer (i.e. 'localhost', '127.0.0.1', or empty string)")]
        public string SshTunnelBoundHost { get; set; }

        [EnabledIf(nameof(UseSshTunnel), HideIfDisabled = true)]
        [Display("Bound Port", Order: 12, Groups: new[] { "SSH Settings", "SSH Tunnel" }, Collapsed: true,
            Description: "local computer")]
        public uint SshTunnelBoundPort { get; set; }

        [EnabledIf(nameof(UseSshTunnel), HideIfDisabled = true)]
        [Display("Forwarded Host", Order: 13, Groups: new[] { "SSH Settings", "SSH Tunnel" }, Collapsed: true,
            Description: "remote server (i.e. 'localhost' or '127.0.0.1')")]
        public string SshTunnelForwardedHost { get; set; }

        [EnabledIf(nameof(UseSshTunnel), HideIfDisabled = true)]
        [Display("Forwarded Port", Order: 14, Groups: new[] { "SSH Settings", "SSH Tunnel" }, Collapsed: true,
            Description: "remote server")]
        public uint SshTunnelForwardedPort { get; set; }

        [Display("Verbose Logging", Order: 20, Group: "Debug", Collapsed: true,
            Description: "Enables verbose logging of SSH communication.")]
        public bool VerboseLoggingEnabled { get; set; } = true;

        #endregion

        private SshClient _sshClient;
        private ForwardedPortLocal _forwardedPortLocal;

        public RaspiSsh()
        {
            // Default SSH Setting values
            Name = nameof(RaspiSsh);
            IpAddress = "192.168.100.1";
            TcpPort = 22;
            Username = "pi";
            Password = "";
            KeepAliveInterval = 30;

            // Default SSH Tunnel values
            UseSshTunnel = false;
            SshTunnelBoundHost = "localhost";
            SshTunnelBoundPort = 4444;
            SshTunnelForwardedHost = "localhost";
            SshTunnelForwardedPort = 4444;

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
            Disconnect();
            base.Close();
            IsConnected = false;
        }

        public void Connect()
        {
            SshConnect();
            StartSshTunnel();
        }

        public void Disconnect()
        {
            StopSshTunnel();
            SshDisconnect();
        }

        public void UploadFiles(List<(string localFile, string remoteFile)> files)
        {
            throw new NotImplementedException();
        }

        public void DownloadFiles(List<(string remoteFile, string localFile)> files)
        {
            throw new NotImplementedException();
        }

        // https://davemateer.com/2021/06/15/ssh-and-sftp-with-ssh-net
        public bool SendSshQuery(string command, int timeout, out string response)
        {
            if (_sshClient == null || !_sshClient.IsConnected)
                throw new InvalidOperationException($"{Name} is not connected");

            var cmd = _sshClient.CreateCommand(command);
            cmd.CommandTimeout = TimeSpan.FromSeconds(timeout);
            Log.Debug($"SSH >> {cmd.CommandText}");

            response = cmd.Execute();
            Log.Debug($"SSH << {response}");

            return cmd.ExitStatus == 0;
        }

        #region Private Methods

        private void SshConnect()
        {
            if (_sshClient == null)
                _sshClient = new SshClient(IpAddress, TcpPort, Username, Password)
                    { KeepAliveInterval = TimeSpan.FromSeconds(KeepAliveInterval) };

            if (_sshClient.IsConnected)
                return;

            if (VerboseLoggingEnabled)
                Log.Debug($"Connecting to {_sshClient.ConnectionInfo.Host}:{_sshClient.ConnectionInfo.Port}");

            _sshClient.Connect();
            IsConnected = true;
        }

        private void SshDisconnect()
        {
            if (_sshClient == null)
                return;

            if (!_sshClient.IsConnected)
                return;

            if (VerboseLoggingEnabled)
                Log.Debug($"Disconnecting from {_sshClient.ConnectionInfo.Host}:{_sshClient.ConnectionInfo.Port}");

            _sshClient.Disconnect();
            _sshClient.Dispose();
            _sshClient = null;
            IsConnected = false;
        }

        private void StartSshTunnel()
        {
            if (!UseSshTunnel)
                return;

            if (_forwardedPortLocal != null && _forwardedPortLocal.IsStarted)
                return;

            _forwardedPortLocal = new ForwardedPortLocal(SshTunnelBoundHost, SshTunnelBoundPort,
                SshTunnelForwardedHost, SshTunnelForwardedPort);

            if (VerboseLoggingEnabled)
                Log.Debug(
                    $"Starting SSH Tunnel {_forwardedPortLocal.BoundHost}:{_forwardedPortLocal.BoundPort}:" +
                    $"{_forwardedPortLocal.Host}:{_forwardedPortLocal.Port}");

            _sshClient.AddForwardedPort(_forwardedPortLocal);
            _forwardedPortLocal.Start();
        }

        private void StopSshTunnel()
        {
            if (_forwardedPortLocal == null)
                return;

            if (!_forwardedPortLocal.IsStarted)
                return;

            if (VerboseLoggingEnabled)
                Log.Debug(
                    $"Stopping SSH Tunnel {_forwardedPortLocal.BoundHost}:{_forwardedPortLocal.BoundPort}:" +
                    $"{_forwardedPortLocal.Host}:{_forwardedPortLocal.Port}");

            _forwardedPortLocal.Stop();
            _sshClient.RemoveForwardedPort(_forwardedPortLocal);
        }

        #endregion
    }
}