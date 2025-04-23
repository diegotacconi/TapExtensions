namespace TapExtensions.Interfaces.Ssh
{
    public interface IRadioAccess
    {
        uint CommandSendTimeoutMs { get; set; }

        uint ConnectionRetryDelay { get; set; }

        bool Connected { get; }

        void Connect(string server, uint port);

        void Close();

        /// <summary> Send command and return raw response. </summary>
        ERadioSuccess SendDataRawResponse(string command, out string response);

        /// <summary> Send command and return parsed response. </summary>
        ERadioSuccess SendData(string command, out string response);

        void SetVerboseLogging(bool verboseLogging);
    }
}