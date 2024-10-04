using System;

namespace TapExtensions.Shared.Telnet
{
    public class TelnetClientEvents
    {
        public event EventHandler<ConnectionEventArgs> Connected;
        public event EventHandler<ConnectionEventArgs> Disconnected;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<DataSentEventArgs> DataSent;

        internal void HandleConnected(object sender, ConnectionEventArgs args)
        {
            Connected?.Invoke(sender, args);
        }

        internal void HandleDisconnected(object sender, ConnectionEventArgs args)
        {
            Disconnected?.Invoke(sender, args);
        }

        internal void HandleDataReceived(object sender, DataReceivedEventArgs args)
        {
            DataReceived?.Invoke(sender, args);
        }

        internal void HandleDataSent(object sender, DataSentEventArgs args)
        {
            DataSent?.Invoke(sender, args);
        }
    }
}