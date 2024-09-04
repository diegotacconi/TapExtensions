using System;
using OpenTap;
using TapExtensions.Interfaces.Spi;

namespace TapExtensions.Instruments.MultipleInterfaces.Aardvark
{
    public partial class Aardvark : ISpi
    {
        #region SPI Interface Implementation

        public void Init(ESpiMode mode, ESpiBitOrder bitOrder, ESpiChipSelect chipSelect,
            ESpiChipSelectPolarity chipSelectPolarity, uint bitRate)
        {
            throw new NotImplementedException();
        }

        public void Init(ESpiMode mode, ESpiBitOrder bitOrder, ESpiSsPolarity eSsPolarity)
        {
            throw new NotImplementedException();
        }

        public byte[] Query(byte[] data)
        {
            throw new NotImplementedException();
        }

        public byte[] Query(byte[] data, ESpiSsSignal eSsSignal)
        {
            throw new NotImplementedException();
        }

        public bool CheckBitRateValueValid(uint bitRatekHz)
        {
            throw new NotImplementedException();
        }

        public void Delay(int value, ESpiSleepMode sleepMode)
        {
            throw new NotImplementedException();
        }

        public void SetBitRate(uint bitrateKhz)
        {
            throw new NotImplementedException();
        }

        public void SlaveEnable()
        {
            throw new NotImplementedException();
        }

        public void SlaveDisable()
        {
            throw new NotImplementedException();
        }

        public byte[] SlaveRead(ushort numOfBytesMax, out int numOfBytesRead)
        {
            throw new NotImplementedException();
        }

        public int SlaveSetResponse(byte[] dataToResponse)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}