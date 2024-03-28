using OpenTap;

namespace TapExtensions.Interfaces.Ssh
{
    public interface ISecureShell : IDut
    {
        string IpAddress { get; }

        int TcpPort { get; }

        string Username { get; }

        string Password { get; }

        void Connect();

        void Disconnect();

        bool Query(string command, int timeout, out string response);
    }
}