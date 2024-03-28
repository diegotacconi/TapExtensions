using System.Collections.Generic;
using OpenTap;

namespace TapExtensions.Interfaces.Ssh
{
    public interface ISecureShell : IDut
    {
        string IpAddress { get; }

        void Connect();

        void Disconnect();

        void UploadFiles(List<(string localFileName, string remoteFileName)> files);

        void DownloadFiles(List<(string remoteFileName, string localFileName)> files);

        bool SendSshQuery(string command, int timeout, out string response);
    }
}