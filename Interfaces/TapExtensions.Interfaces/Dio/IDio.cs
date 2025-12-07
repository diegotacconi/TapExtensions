using System.Collections.Generic;
using OpenTap;

namespace TapExtensions.Interfaces.Dio
{
    public interface IDio : IInstrument
    {
        List<EInputState> GetInputState(List<short> channels);

        void SetOutputState(List<short> channels, List<EOutputState> states);
    }

    #region Enums

    public enum EInputState
    {
        Low = 0,
        High = 1,
        Middle = 3
    }

    public enum EOutputState
    {
        Sink,
        Source,
        Off
    }

    #endregion
}