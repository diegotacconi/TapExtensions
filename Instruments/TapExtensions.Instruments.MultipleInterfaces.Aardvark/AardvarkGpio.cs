using System;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Instruments.MultipleInterfaces.Aardvark
{
    public partial class Aardvark : IGpio
    {
        #region GPIO Interface Implementation

        public void SetPinState(int pin, EPinState state)
        {
            throw new NotImplementedException();
        }

        public EPinState GetPinState(int pin)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}