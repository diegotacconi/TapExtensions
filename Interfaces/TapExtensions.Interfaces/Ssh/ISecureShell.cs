using System.Collections.Generic;
using OpenTap;

namespace TapExtensions.Interfaces.Ssh
{
    public interface ISecureShell : IDut
    {
        string IpAddress { get; }

        void Connect();

        void Disconnect();

        void UploadFiles(List<(string localFilename, string remoteFilename)> files);

        void DownloadFiles(List<(string remoteFilename, string localFilename)> files);

        bool SendSshQuery(string command, int timeout, out string response);
    }
}