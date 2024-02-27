using OpenTap;

namespace TapExtensions.Interfaces.Duts
{
    public interface IDutControlSsh : IDut
    {
        void ConnectDut();

        void DisconnectDut();

        string SendSshQuery(string command, int timeout);
    }
}