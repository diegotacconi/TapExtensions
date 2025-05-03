using OpenTap;

namespace TapExtensions.Interfaces.Ssh
{
    public interface ISshInstrument : IInstrument
    {
        void Connect();

        void Disconnect();

        bool SendSshQuery(string command, int timeout, out string response);
    }
}