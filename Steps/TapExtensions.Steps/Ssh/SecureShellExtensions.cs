using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Ssh
{
    internal static class SecureShellExtensions
    {
        public static string Query(this ISecureShell dut, string command, int timeout)
        {
            dut.SendSshQuery(command, timeout, out var response);
            return response;
        }
    }
}