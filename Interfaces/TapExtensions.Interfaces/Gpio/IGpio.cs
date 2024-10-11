using OpenTap;

namespace TapExtensions.Interfaces.Gpio
{
    public interface IGpio : IInstrument
    {
        void SetPinMode(int pin, EPinInputMode mode);

        void SetPinState(int pin, EPinState state);

        EPinState GetPinState(int pin);
    }

    public enum EPinState
    {
        OutputHigh,
        OutputLow,
        Input
    }

    public enum EPinInputMode
    {
        PullUp,
        PullDown,
        PullNone
    }
}