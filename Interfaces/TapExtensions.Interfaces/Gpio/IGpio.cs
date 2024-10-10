namespace TapExtensions.Interfaces.Gpio
{
    public interface IGpio
    {
        void SetPinMode(int pin, EPinMode mode);

        void SetPinState(int pin, EPinState state);

        EPinState GetPinState(int pin);
    }

    public enum EPinState
    {
        High = 1,
        Low = 0
    }

    public enum EPinMode
    {
        Input,
        InputPullDown,
        InputPullUp,
        Output
    }
}