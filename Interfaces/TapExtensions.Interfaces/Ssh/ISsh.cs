using System.Collections.Generic;
using OpenTap;

namespace TapExtensions.Interfaces.Ssh
{
    /// <summary> Secure Shell Protocol (SSH) </summary>
    public interface ISsh : IDut
    {
        void Connect();

        void Disconnect();

        string Query(string command, int timeout);

        /// <summary>
        ///     Upload file(s) from PC to DUT, using the Secure Copy Protocol (SCP)
        /// </summary>
        void UploadFiles(List<(string localFilePath, string remoteFilePath)> files);

        /// <summary>
        ///     Download file(s) from DUT to PC, using the Secure Copy Protocol (SCP)
        /// </summary>
        void DownloadFiles(List<(string remoteFilePath, string localFilePath)> files);
    }
}