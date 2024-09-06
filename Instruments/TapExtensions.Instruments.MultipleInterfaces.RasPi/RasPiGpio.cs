using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Instruments.MultipleInterfaces.RasPi
{
    [Display("RasPiGpio",
        Groups: new[] { "TapExtensions", "Instruments", "MultipleInterfaces" })]
    public class RasPiGpio : Resource, IGpio
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
}