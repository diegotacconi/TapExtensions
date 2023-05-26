using OpenTap;

namespace TapExtensions.Instruments.DcPwr
{
    [Display("Keysight E4356A",
        Groups: new[] { "TapExtensions", "Instruments", "DcPwr" },
        Description: "Keysight E4356A Telecom DC Power Supply, 70V, 30A and 80V, 26A")]
    public class KeysightE4356A : KeysightDcPwr
    {
        public KeysightE4356A()
        {
            // Default values
            Name = nameof(KeysightE4356A);
            VerboseLoggingEnabled = true;
        }

        public override void Open()
        {
            Open("4356A", Name);
            GetMaxMinValues();
        }
    }
}