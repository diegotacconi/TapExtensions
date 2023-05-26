using OpenTap;

namespace TapExtensions.Instruments.DcPwr
{
    [Display("Keysight E3632A",
        Groups: new[] { "TapExtensions", "Instruments", "DcPwr" },
        Description: "Keysight E3632A Single-output Dual-range DC Power Supply")]
    public class KeysightE3632A : KeysightDcPwr
    {
        public enum EVoltageRange
        {
            [Display("15V/7A")] P15V,
            [Display("30V/4A")] P30V
        }

        [Display("Voltage Range", Group: "Open Parameters", Order: 5)]
        public EVoltageRange VoltageRangeChoice { get; set; }

        public KeysightE3632A()
        {
            // Default values
            Name = nameof(KeysightE3632A);
            VerboseLoggingEnabled = true;
            VoltageRangeChoice = EVoltageRange.P15V;
        }

        public override void Open()
        {
            Open("E3632A", Name);
            SetVoltageRange(VoltageRangeChoice.ToString());
            GetMaxMinValues();
        }
    }
}