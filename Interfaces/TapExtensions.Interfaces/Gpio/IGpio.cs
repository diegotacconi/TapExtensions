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

    // ERaspiGpio is only used on Raspberry Pi test steps
    // ReSharper disable InconsistentNaming
    public enum ERaspiGpio
    {
        GPIO_02_PINHDR_03 = 2,
        GPIO_03_PINHDR_05 = 3,
        GPIO_04_PINHDR_07 = 4,
        GPIO_05_PINHDR_29 = 5,
        GPIO_06_PINHDR_31 = 6,
        GPIO_07_PINHDR_26 = 7,
        GPIO_08_PINHDR_24 = 8,
        GPIO_09_PINHDR_21 = 9,
        GPIO_10_PINHDR_19 = 10,
        GPIO_11_PINHDR_23 = 11,
        GPIO_12_PINHDR_32 = 12,
        GPIO_13_PINHDR_33 = 13,
        GPIO_14_PINHDR_08 = 14,
        GPIO_15_PINHDR_10 = 15,
        GPIO_16_PINHDR_36 = 16,
        GPIO_17_PINHDR_11 = 17,
        GPIO_18_PINHDR_12 = 18,
        GPIO_19_PINHDR_35 = 19,
        GPIO_20_PINHDR_38 = 20,
        GPIO_21_PINHDR_40 = 21,
        GPIO_22_PINHDR_15 = 22,
        GPIO_23_PINHDR_16 = 23,
        GPIO_24_PINHDR_18 = 24,
        GPIO_25_PINHDR_22 = 25,
        GPIO_26_PINHDR_37 = 26,
        GPIO_27_PINHDR_13 = 27
    }
}