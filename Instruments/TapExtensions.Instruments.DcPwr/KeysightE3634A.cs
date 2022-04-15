using OpenTap;

namespace TapExtensions.Instruments.DcPwr
{
    [Display("Keysight E3634A",
        Groups: new[] {"TapExtensions", "Instruments", "DcPwr"},
        Description: "Keysight E3634A Single-output Dual-range DC Power Supply")]
    public class KeysightE3634A : KeysightE3630Base
    {
        public enum EVoltageRange
        {
            [Display("25V/7A")] P25V,
            [Display("50V/4A")] P50V
        }

        [Display("Voltage Range", Group: "Open Parameters", Order: 5)]
        public EVoltageRange VoltageRangeChoice { get; set; }

        public KeysightE3634A()
        {
            // Default values
            Name = nameof(KeysightE3634A);
            VoltageRangeChoice = EVoltageRange.P50V;
        }

        public override void Open()
        {
            Open("E3634A", Name, VoltageRangeChoice.ToString());
        }
    }
}