using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Instruments.MultipleInterfaces.Aardvark
{
    public partial class Aardvark : IGpio
    {
        #region GPIO Interface Implementation

        public void SetPinState(int pin, EPinState state)
        {
            Log.Debug($"Setting pin '{GetPinName(pin)}' to state '{state}'");
        }

        public EPinState GetPinState(int pin)
        {
            const EPinState state = EPinState.Low;
            Log.Debug($"Getting pin '{GetPinName(pin)}' return state of '{state}'");
            return state;
        }

        public void SetPinMode(int pin, EPinMode mode)
        {
            Log.Debug($"Setting pin '{GetPinName(pin)}' to mode '{mode}'");

            // SetPinDirection (Input/Output)
            // SetPinPullup Resistor (On/Off)
        }

        #endregion

        #region Private Methods

        private static string GetPinName(int pin)
        {
            var pinName = Enum.IsDefined(typeof(EAardvarkPin), pin)
                ? $"{(EAardvarkPin)pin} (0x{pin:X})"
                : $"0x{pin:X}";

            return pinName;
        }

        #endregion
    }
}