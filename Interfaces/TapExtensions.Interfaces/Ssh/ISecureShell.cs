using System.Collections.Generic;
using OpenTap;

namespace TapExtensions.Interfaces.Ssh
{
    public interface ISecureShell : IDut
    {
        string IpAddress { get; }

        void Connect();

        void Disconnect();

        bool Ping(uint timeoutMs, uint retryIntervalMs, uint minSuccessfulReplies);

        void UploadFiles(List<(string localFile, string remoteFile)> files);

        void DownloadFiles(List<(string remoteFile, string localFile)> files);

        bool SendSshQuery(string command, int timeout, out string response);
    }
}