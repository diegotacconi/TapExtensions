using OpenTap;

namespace TapExtensions.Interfaces.Duts
{
    public interface IDutControlSsh : IDut
    {
        void ConnectDut();

        void DisconnectDut();

        void SendSshCommands(string[] commands, int timeoutMs);

        void SendSshCommands(string[] commands, int timeoutMs, bool runAsRoot);

        string SendSshQuery(string command, int timeoutMs);
    }
}