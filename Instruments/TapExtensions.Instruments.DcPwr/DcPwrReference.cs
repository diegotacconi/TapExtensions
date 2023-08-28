using OpenTap;
using TapExtensions.Interfaces.Common;
using TapExtensions.Interfaces.DcPwr;

namespace TapExtensions.Instruments.DcPwr
{
    [Display("DcPwrReference",
        Groups: new[] { "TapExtensions", "Instruments", "DcPwr" },
        Description: "Reference another instrument")]
    public class DcPwrReference : Instrument, IDcPwr
    {
        [Display("DcPwr",
            Description: "DC Power Supply instrument interface")]
        public IDcPwr DcPwr { get; set; }

        public DcPwrReference()
        {
            Name = nameof(DcPwrReference);
        }

        public EState GetOutputState()
        {
            return DcPwr.GetOutputState();
        }

        public double MeasureCurrent()
        {
            return DcPwr.MeasureCurrent();
        }

        public double MeasureVoltage()
        {
            return DcPwr.MeasureVoltage();
        }

        public void SetCurrent(double currentAmps)
        {
            DcPwr.SetCurrent(currentAmps);
        }

        public void SetVoltage(double voltageVolts)
        {
            DcPwr.SetVoltage(voltageVolts);
        }

        public void SetOutputState(EState state)
        {
            DcPwr.SetOutputState(state);
        }
    }
}