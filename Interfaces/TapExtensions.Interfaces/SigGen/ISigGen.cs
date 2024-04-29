using OpenTap;
using TapExtensions.Interfaces.Common;

namespace TapExtensions.Interfaces.SigGen
{
    public interface ISigGen : IInstrument
    {
        double GetFrequency();

        double GetOutputLevel();

        EState GetRfOutputState();

        void SetFrequency(double frequencyMhz);

        void SetOutputLevel(double outputLevelDbm);

        void SetRfOutputState(EState state);
    }
}