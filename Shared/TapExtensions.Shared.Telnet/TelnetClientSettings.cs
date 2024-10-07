using System;
using System.Net;

namespace TapExtensions.Shared.Telnet
{
    public class TelnetClientSettings
    {
        /// <summary> The System.Net.IPEndPoint to which you bind the TCP System.Net.Sockets.Socket. </summary>
        public IPEndPoint LocalEndpoint { get; set; }

        /// <summary>
        ///     Disable Nagle's algorithm
        ///     Gets or sets a value that disables a delay when send or receive buffers are not full.
        ///     True if the delay is disabled, otherwise false.
        /// </summary>
        public bool NoDelay { get; set; } = true;

        /// <summary> Buffer size to use while interacting with streams. </summary>
        public int StreamBufferSize
        {
            get => _streamBufferSize;
            set
            {
                if (value < 1) throw new ArgumentException("StreamBufferSize must be one or greater.");
                if (value > 65536) throw new ArgumentException("StreamBufferSize must be less than 65,536.");
                _streamBufferSize = value;
            }
        }

        private int _streamBufferSize = 65536;

        /// <summary> The number of milliseconds to wait when attempting to connect. </summary>
        public int ConnectTimeoutMs
        {
            get => _connectTimeoutMs;
            set
            {
                if (value < 1) throw new ArgumentException("ConnectTimeoutMs must be greater than zero.");
                _connectTimeoutMs = value;
            }
        }

        private int _connectTimeoutMs = 5000;

        /// <summary> The number of milliseconds to wait when attempting to read before returning null. </summary>
        public int ReadTimeoutMs
        {
            get => _readTimeoutMs;
            set
            {
                if (value < 1) throw new ArgumentException("ReadTimeoutMs must be greater than zero.");
                _readTimeoutMs = value;
            }
        }

        private int _readTimeoutMs = 1000;

        /// <summary>
        ///     Maximum amount of time to wait before considering the server to be idle and disconnecting from it.
        ///     By default, this value is set to 0, which will never disconnect due to inactivity.
        ///     The timeout is reset any time a message is received from the server.
        ///     For instance, if you set this value to 30000, the client will disconnect if the server has not sent a message to
        ///     the client within 30 seconds.
        /// </summary>
        public int IdleServerTimeoutMs
        {
            get => _idleServerTimeoutMs;
            set
            {
                if (value < 0) throw new ArgumentException("IdleClientTimeoutMs must be zero or greater.");
                _idleServerTimeoutMs = value;
            }
        }

        private int _idleServerTimeoutMs;

        /// <summary>
        ///     Number of milliseconds to wait between each iteration of evaluating the server connection to see if the configured
        ///     timeout interval has been exceeded.
        /// </summary>
        public int IdleServerEvaluationIntervalMs
        {
            get => _idleServerEvaluationIntervalMs;
            set
            {
                if (value < 1) throw new ArgumentException("IdleServerEvaluationIntervalMs must be one or greater.");
                _idleServerEvaluationIntervalMs = value;
            }
        }

        private int _idleServerEvaluationIntervalMs = 1000;

        /// <summary>
        ///     Number of milliseconds to wait between each iteration of evaluating the server connection to see if the connection
        ///     is lost.
        /// </summary>
        public int ConnectionLostEvaluationIntervalMs
        {
            get => _connectionLostEvaluationIntervalMs;
            set
            {
                if (value < 1)
                    throw new ArgumentException("ConnectionLostEvaluationIntervalMs must be one or greater.");
                _connectionLostEvaluationIntervalMs = value;
            }
        }

        private int _connectionLostEvaluationIntervalMs = 200;

        /// <summary>
        ///     Enable or disable whether the data receiver thread fires the DataReceived event from a background task.
        ///     The default is enabled.
        /// </summary>
        public bool UseAsyncDataReceivedEvents = true;
    }
}