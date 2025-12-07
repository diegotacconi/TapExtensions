using System;
using System.Collections.Generic;
using OpenTap;
using TapExtensions.Interfaces.Dio;

namespace TapExtensions.Instruments.Dio
{
    [Display("DioDummy",
        Groups: new[] { "TapExtensions", "Instruments", "Dio" })]
    public class DioDummy : Instrument, IDio
    {
        public DioDummy()
        {
            Name = "DioDummy";
        }

        public List<EInputState> GetInputState(List<short> channels)
        {
            throw new NotImplementedException();
        }

        public void SetOutputState(List<short> channels, List<EOutputState> states)
        {
            var msg = $"{nameof(SetOutputState)}: ";
            for (var i = 0; i < channels.Count; i++)
                msg += $"({channels[i]},{states[i]}) ";
            Log.Debug(msg);
        }
    }
}