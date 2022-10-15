using System.Collections.Generic;
using OpenTap;

namespace TapExtensions.Interfaces.Ssh
{
    public interface ISsh : IDut
    {
        void Connect();

        void Disconnect();

        string Query(string command, int timeout);
    }
}