using System;
using OpenTap;

namespace TapExtensions.Instruments.DcPwr
{
    [Display("Keysight E3633A",
        Groups: new[] { "TapExtensions", "Instruments", "DcPwr" },
        Description: "Keysight E3633A Single-output Dual-range DC Power Supply")]
    public class KeysightE3633A : KeysightDcPwr
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
            VerboseLoggingEnabled = true;
            VoltageRangeChoice = EVoltageRange.P20V;
        }

        public override void Open()
        {
            Open("E3633A", Name);

            // Check voltage range
            var voltageRangeChoice = VoltageRangeChoice.ToString();
            var voltageRange = ScpiQuery<string>("VOLTage:RANGe?");

            if (voltageRange != voltageRangeChoice)
            {
                // Select voltage range
                ScpiCommand($"VOLTage:RANGe {voltageRangeChoice}");

                // Verify voltage range selection
                voltageRange = ScpiQuery<string>("VOLTage:RANGe?");
                if (voltageRange != voltageRangeChoice)
                    throw new InvalidOperationException("Unable to select desired voltage range.");
            }
        }
    }
}