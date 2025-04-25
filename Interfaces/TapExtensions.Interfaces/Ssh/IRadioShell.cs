namespace TapExtensions.Interfaces.Ssh
{
    public interface IRadioShell : ISecureShell
    {
        void ConnectDutRadio(uint timeoutMs, uint minSuccessfulPingReplies);

        IRadioAccess CreateRadioAccess(string userName, string password, uint port, uint timeOut, bool verboseLogging);

        ERadioSuccess SendRadioCommand(string command, out string response, bool returnErrorCode = false);

        string CreateAgentPath(string agentName, string devicePath = "");

        void TerminateAgent(string path);
    }

    public enum ERadioSuccess
    {
        Ok,
        Failed,
        Error
    }
}