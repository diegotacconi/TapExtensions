using OpenTap;
using TapExtensions.Interfaces.Common;

namespace TapExtensions.Interfaces.DcPwr
{
    public interface IDcPwr : IInstrument
    {
        EState GetOutputState();

        double MeasureCurrent();

        double MeasureVoltage();

        void SetCurrent(double currentAmps);

        void SetVoltage(double voltageVolts);

        void SetOutputState(EState state);
    }
}