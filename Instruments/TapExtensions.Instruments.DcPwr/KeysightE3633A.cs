using OpenTap;

namespace TapExtensions.Instruments.DcPwr
{
    [Display("Keysight E3633A",
        Groups: new[] {"TapExtensions", "Instruments", "DcPwr"},
        Description: "Keysight E3633A Single-output Dual-range DC Power Supply")]
    public class KeysightE3633A : KeysightE3630Base
    {
        public enum EVoltageRange
        {
            [Display("8V/20A")] P8V,
            [Display("20V/10A")] P20V
        }

        [Display("Voltage Range", Group: "Open Parameters", Order: 5)]
        public EVoltageRange VoltageRangeChoice { get; set; }

        public KeysightE3633A()
        {
            // Default values
            Name = nameof(KeysightE3633A);
            VoltageRangeChoice = EVoltageRange.P20V;
        }

        public override void Open()
        {
            Open("E3633A", Name, VoltageRangeChoice.ToString());
        }
    }
}