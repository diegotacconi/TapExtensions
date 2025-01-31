using OpenTap;

namespace TapExtensions.Interfaces.Gpio
{
    public interface IGpio : IGpioDevice, IInstrument
    {
    }

    public interface IGpioDevice
    {
        void SetPinDirection(int pin, EDirection direction);

        void SetPinPull(int pin, EPull pull);

        void SetPinDrive(int pin, EDrive drive);

        ELevel GetPinLevel(int pin);

        void SetPin(int pin, EDirection direction, EPull pull);

        void SetPin(int pin, EDirection direction, EPull pull, EDrive drive);

        (EDirection direction, EPull pull, ELevel level) GetPin(int pin);
    }

    public enum EDirection
    {
        Input,
        Output
    }

    public enum EPull
    {
        PullNone,
        PullDown,
        PullUp
    }

    public enum EDrive
    {
        DriveLow,
        DriveHigh
    }

    public enum ELevel
    {
        Low,
        High
    }
}