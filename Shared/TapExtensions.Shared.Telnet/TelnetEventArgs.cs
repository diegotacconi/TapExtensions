using System;

namespace TapExtensions.Shared.Telnet
{
    public enum DisconnectReason
    {
        /// <summary> Normal disconnection. </summary>
        Normal = 0,

        /// <summary> Client connection was intentionally terminated programmatically or by the server. </summary>
        Kicked = 1,

        /// <summary> Client connection timed out; server did not receive data within the timeout window. </summary>
        Timeout = 2,

        /// <summary> The connection was not disconnected. </summary>
        None = 3
    }

    public class ConnectionEventArgs : EventArgs
    {
        internal ConnectionEventArgs(string ipPort, DisconnectReason reason = DisconnectReason.None)
        {
            IpPort = ipPort;
            Reason = reason;
        }

        public string IpPort { get; }

        public DisconnectReason Reason { get; }
    }

    public class DataSentEventArgs : EventArgs
    {
        internal DataSentEventArgs(string ipPort, long bytesSent)
        {
            IpPort = ipPort;
            BytesSent = bytesSent;
        }

        public string IpPort { get; }

        /// <summary> The number of bytes sent. </summary>
        public long BytesSent { get; }
    }

    public class DataReceivedEventArgs : EventArgs
    {
        internal DataReceivedEventArgs(string ipPort, ArraySegment<byte> data)
        {
            IpPort = ipPort;
            Data = data;
        }

        public string IpPort { get; }

        /// <summary> The data received from the endpoint. </summary>
        public ArraySegment<byte> Data { get; }
    }
}