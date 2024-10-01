using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Instruments.MultipleInterfaces.Raspi
{
    [Display("RaspiGpio",
        Groups: new[] { "TapExtensions", "Instruments", "MultipleInterfaces" })]
    public class RaspiGpio : Resource, IGpio
    {
        public void SetPinMode(int pin, EPinMode mode)
        {
            throw new NotImplementedException();
        }

        public void SetPinState(int pin, EPinState state)
        {
            // ToDo:
            //    /sys/class/gpio/gpio11/direction
            //    /sys/class/gpio/gpio11/value
            //    /dev/gpiochipN
            //    sudo usermod -a -G gpio <username>

            throw new NotImplementedException();
        }

        public EPinState GetPinState(int pin)
        {
            throw new NotImplementedException();
        }
    }

    // ReSharper disable InconsistentNaming
    public enum ERaspiPin
    {
        GPIO_02_PINHDR_03 = 3,
        GPIO_03_PINHDR_05 = 5,
        GPIO_04_PINHDR_07 = 7,
        GPIO_14_PINHDR_08 = 8,
        GPIO_15_PINHDR_10 = 10,
        GPIO_17_PINHDR_11 = 11,
        GPIO_18_PINHDR_12 = 12,
        GPIO_27_PINHDR_13 = 13,
        GPIO_22_PINHDR_15 = 15,
        GPIO_23_PINHDR_16 = 16,
        GPIO_24_PINHDR_18 = 18,
        GPIO_10_PINHDR_19 = 19,
        GPIO_09_PINHDR_21 = 21,
        GPIO_25_PINHDR_22 = 22,
        GPIO_11_PINHDR_23 = 23,
        GPIO_08_PINHDR_24 = 24,
        GPIO_07_PINHDR_26 = 26,
        GPIO_05_PINHDR_29 = 29,
        GPIO_06_PINHDR_31 = 31,
        GPIO_12_PINHDR_32 = 32,
        GPIO_13_PINHDR_33 = 33,
        GPIO_19_PINHDR_35 = 35,
        GPIO_16_PINHDR_36 = 36,
        GPIO_26_PINHDR_37 = 37,
        GPIO_20_PINHDR_38 = 38,
        GPIO_21_PINHDR_40 = 40
    }
}