using OpenTap;

namespace TapExtensions.Interfaces.Common
{
    public enum EState
    {
        NotSet,
        [Scpi("OFF")] Off,
        [Scpi("ON")] On
    }
}