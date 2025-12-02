using OpenTap;
using TapExtensions.Interfaces.Switch;

namespace TapExtensions.Instruments.Switch
{
    [Display("SwitchDummy",
        Groups: new[] { "TapExtensions", "Instruments", "Switch" })]
    public class SwitchDummy : Instrument, ISwitch
    {
        public SwitchDummy()
        {
            Name = "SwitchDummy";
        }

        public void SetRoute(string routeName)
        {
            Log.Info($"SetRoute({routeName})");
        }
    }
}