using OpenTap;

namespace TapExtensions.Instruments.DcPwr
{
    [Display("Keysight 6032A",
        Groups: new[] { "TapExtensions", "Instruments", "DcPwr" },
        Description: "Keysight 6032A DC Power Supply, 60V, 50A")]
    public class Keysight6032A : KeysightDcPwr
    {
        public Keysight6032A()
        {
            // Default values
            Name = nameof(Keysight6032A);
            VerboseLoggingEnabled = true;
        }

        public override void Open()
        {
            Open("6032A", Name);
            GetMaxMinValues();
        }
    }
}