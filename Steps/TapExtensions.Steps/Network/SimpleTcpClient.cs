using System;
using System.Net.Sockets;
using System.Text;
using OpenTap;

namespace TapExtensions.Steps.Network
{
    [Display("SimpleTcpClient",
        Groups: new[] { "TapExtensions", "Steps", "Network" })]
    public class SimpleTcpClient : TestStep
    {
        [Display("IP Address", Order: 1)] public string IpAddress { get; set; } = "localhost";

        [Display("Port", Order: 2)] public int TcpPort { get; set; } = 4444;

        private TcpClient _tcpClient;
        private NetworkStream _tcpStream;
        private bool IsOpen => _tcpClient != null && _tcpClient.Connected;

        public override void Run()
        {
            try
            {
                Connect();
                try
                {
                    WriteExpect("version", ">");
                    Write("exit");
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

        public void Connect()
        {
            if (IsOpen)
                return;

            Log.Debug($"Connecting to {IpAddress}:{TcpPort}");
            _tcpClient = new TcpClient(IpAddress, TcpPort);
            _tcpStream = _tcpClient.GetStream();
            _tcpStream.ReadTimeout = 5000;
            _tcpStream.WriteTimeout = 5000;
        }

        public void Disconnect()
        {
            if (!IsOpen)
                return;

            Log.Debug($"Disconnecting from {IpAddress}:{TcpPort}");
            _tcpStream?.Close();
            _tcpClient?.Close();
        }

        public void Write(string command)
        {
            if (_tcpStream.CanWrite)
            {
                var bytes = Encoding.ASCII.GetBytes(command);
                _tcpStream.Write(bytes, 0, bytes.Length);
                // _stream.Flush();
                Log.Debug($"TCP >> {command}");
            }
        }

        public string Read()
        {
            var response = new StringBuilder();
            if (_tcpStream.CanRead)
            {
                var buffer = new byte[1024];
                do
                {
                    var count = _tcpStream.Read(buffer, 0, buffer.Length);
                    response.Append(Encoding.ASCII.GetString(buffer, 0, count));
                } while (_tcpStream.DataAvailable);

                Log.Debug($"TCP << {response}");
            }

            return response.ToString().Trim();
        }

        public void WriteExpect(string command, string expectedResponse)
        {
            Write(command);
            TapThread.Sleep(100);
            var response = Read();

            if (!response.Contains(expectedResponse))
                throw new InvalidOperationException(
                    $"Cannot find '{expectedResponse}' in the response to the command of '{command}'");
        }
    }
}