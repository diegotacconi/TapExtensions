using System;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Duts.RadioShell
{
    public static class RadioParser
    {
        private const string StatDoneOk = "==> done ok";
        private const string StatDoneFailed = "==> done failed";
        private const string StatError = "==> error";

        /// <summary> Parses the verdict from response </summary>
        public static ERadioSuccess ParseResponseVerdict(string responseString, TraceSource logger)
        {
            ERadioSuccess eSuccess;

            if (responseString.IndexOf(StatDoneOk, StringComparison.InvariantCulture) > -1)
            {
                eSuccess = ERadioSuccess.Ok;
            }
            else if (responseString.IndexOf(StatDoneFailed, StringComparison.InvariantCulture) > -1)
            {
                eSuccess = ERadioSuccess.Failed;
            }
            else if (responseString.IndexOf(StatError, StringComparison.InvariantCulture) > -1)
            {
                eSuccess = ERadioSuccess.Error;
            }
            else
            {
                logger?.Error("Unknown radio response.");
                eSuccess = ERadioSuccess.Error;
            }

            return eSuccess;
        }

        /// <summary> Parses the radio response, returns status and cleaned up response string </summary>
        public static ERadioSuccess ParseResponse(string rawResponse, out string parsedResponse, TraceSource logger)
        {
            ERadioSuccess eSuccess;

            int startIndex;

            if ((startIndex = rawResponse.IndexOf(StatDoneOk, StringComparison.InvariantCulture)) > -1)
            {
                TrimResponse(startIndex + StatDoneOk.Length, rawResponse, out parsedResponse);
                eSuccess = ERadioSuccess.Ok;
            }
            else if ((startIndex = rawResponse.IndexOf(StatDoneFailed, StringComparison.InvariantCulture)) > -1)
            {
                TrimResponse(startIndex + StatDoneFailed.Length, rawResponse, out parsedResponse);
                eSuccess = ERadioSuccess.Failed;
            }
            else if ((startIndex = rawResponse.IndexOf(StatError, StringComparison.InvariantCulture)) > -1)
            {
                TrimResponse(startIndex + StatError.Length, rawResponse, out parsedResponse);
                // ignore error 0x6d
                const string ignoreError = "path already taken";
                eSuccess = parsedResponse.IndexOf(ignoreError, StringComparison.InvariantCultureIgnoreCase) >= 0
                    ? ERadioSuccess.Ok
                    : ERadioSuccess.Error;
            }
            else
            {
                logger?.Error("Unknown radio response.");
                eSuccess = ERadioSuccess.Error;
                parsedResponse = string.Empty;
            }

            return eSuccess;
        }

        private static void TrimResponse(int startIndex, string rawResponse, out string parsedResponse)
        {
            char[] quoteChars = { '\'', '\"' };
            char[] charsToTrim = { '\r', '\n', '\0' };

            // ==> done ok L0123456789
            // ==> done ok\r\n\0 also possible
            // ==> done failed 0xff create device failed
            // ==> error 0x03 Invalid <argument>: out of 0...100000 range
            parsedResponse = rawResponse.Substring(startIndex).Trim().Trim(charsToTrim);
            parsedResponse = parsedResponse.Trim(quoteChars);
        }
    }
}