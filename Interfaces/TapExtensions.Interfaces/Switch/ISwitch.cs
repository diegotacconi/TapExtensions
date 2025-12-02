using OpenTap;

namespace TapExtensions.Interfaces.Switch
{
    public interface ISwitch : IInstrument
    {
        void SetRoute(string routeName);
    }
}