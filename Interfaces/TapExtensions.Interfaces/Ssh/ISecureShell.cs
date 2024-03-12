using OpenTap;

namespace TapExtensions.Interfaces.Ssh
{
    public interface ISecureShell : IDut
    {
        void Connect();

        void Disconnect();

        bool Query(string command, int timeout, out string response);
    }
}