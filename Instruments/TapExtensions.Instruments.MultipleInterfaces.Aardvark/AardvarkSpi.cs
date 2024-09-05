using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using OpenTap;
using TapExtensions.Interfaces.Spi;

namespace TapExtensions.Instruments.MultipleInterfaces.Aardvark
{
    public partial class Aardvark : ISpi
    {
        // TotalPhase/aardvark-v5.15.pdf/Chapter5.5.1/4
        // It is not possible to receive messages larger than approximately 4 KiB as a slave
        // due to operating system limitations on the asynchronous incoming buffer. As such,
        // one should not queue up more than 4 KiB of total slave data between calls to the Aardvark API.
        private const ushort MaxTxRxBytes = 4000;

        #region SPI Interface Implementation

        void ISpi.Init(ESpiMode mode, ESpiBitOrder bitOrder, ESpiChipSelect chipSelect,
            ESpiChipSelectPolarity chipSelectPolarity, uint bitRate)
        {
            if (!Enum.IsDefined(typeof(ESpiMode), mode))
                throw new InvalidEnumArgumentException(nameof(mode), (int)mode, typeof(ESpiMode));
            if (!Enum.IsDefined(typeof(ESpiBitOrder), bitOrder))
                throw new InvalidEnumArgumentException(nameof(bitOrder), (int)bitOrder, typeof(ESpiBitOrder));
            if (!Enum.IsDefined(typeof(ESpiChipSelect), chipSelect))
                throw new InvalidEnumArgumentException(nameof(chipSelect), (int)chipSelect, typeof(ESpiChipSelect));
            if (!Enum.IsDefined(typeof(ESpiChipSelectPolarity), chipSelectPolarity))
                throw new InvalidEnumArgumentException(nameof(chipSelectPolarity), (int)chipSelectPolarity,
                    typeof(ESpiChipSelectPolarity));

            if (!CheckBitRateValueValid(bitRate))
                throw new ArgumentOutOfRangeException(nameof(bitRate));

            lock (_instLock)
            {
                ((ISpi)this).SetBitRate(bitRate);

                AardvarkSpiPolarity spiPolar;
                AardvarkSpiPhase spiPhase;
                switch (mode)
                {
                    case ESpiMode.Mode0:
                        spiPolar = AardvarkSpiPolarity.AA_SPI_POL_RISING_FALLING;
                        spiPhase = AardvarkSpiPhase.AA_SPI_PHASE_SAMPLE_SETUP;
                        break;
                    case ESpiMode.Mode1:
                        spiPolar = AardvarkSpiPolarity.AA_SPI_POL_RISING_FALLING;
                        spiPhase = AardvarkSpiPhase.AA_SPI_PHASE_SETUP_SAMPLE;
                        break;
                    case ESpiMode.Mode2:
                        spiPolar = AardvarkSpiPolarity.AA_SPI_POL_FALLING_RISING;
                        spiPhase = AardvarkSpiPhase.AA_SPI_PHASE_SAMPLE_SETUP;
                        break;
                    case ESpiMode.Mode3:
                        spiPolar = AardvarkSpiPolarity.AA_SPI_POL_FALLING_RISING;
                        spiPhase = AardvarkSpiPhase.AA_SPI_PHASE_SETUP_SAMPLE;
                        break;
                    default:
                        throw new ArgumentException("SPI Init \"mode\"-parameter: " + mode + " is not allowed.",
                            nameof(mode));
                }

                var spiBitOrder = AardvarkSpiBitorder.AA_SPI_BITORDER_LSB;
                if (bitOrder == ESpiBitOrder.Msb)
                    spiBitOrder = AardvarkSpiBitorder.AA_SPI_BITORDER_MSB;

                var stat = AardvarkWrapper.aa_spi_configure(AardvarkHandle, spiPolar, spiPhase, spiBitOrder);
                if (stat != (int)AardvarkStatus.AA_OK)
                    throw new ApplicationException("SPI set configure(spiPolar:" + spiPolar + ", spiPhase:" + spiPhase +
                                                   ", spiBitOrder:" + spiBitOrder + ") return: " + stat);

                var spiSsPolar = AardvarkSpiSSPolarity.AA_SPI_SS_ACTIVE_LOW;
                if (chipSelectPolarity == ESpiChipSelectPolarity.ActiveHigh)
                    spiSsPolar = AardvarkSpiSSPolarity.AA_SPI_SS_ACTIVE_HIGH;

                stat = AardvarkWrapper.aa_spi_master_ss_polarity(AardvarkHandle, spiSsPolar);
                if (stat != (int)AardvarkStatus.AA_OK)
                    throw new ApplicationException("SPI set master_ss_polarity(" + spiSsPolar + ") return: " + stat);
            }
        }

        void ISpi.Init(ESpiMode mode, ESpiBitOrder bitOrder, ESpiSsPolarity eSsPolarity)
        {
            throw new NotImplementedException();
        }

        byte[] ISpi.Query(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length > MaxTxRxBytes)
                throw new ArgumentException("Data size cannot be bigger than " + MaxTxRxBytes + ".", nameof(data));

            CheckIfInitialized();
            lock (_instLock)
            {
                var dataLength = Convert.ToUInt16(data.Length);
                LogDebugData("SPI Master >> ", data);

                var dataReceiv = new byte[dataLength];
                var stat = AardvarkWrapper.aa_spi_write(AardvarkHandle, dataLength, data,
                    dataLength, dataReceiv);
                if (stat < 0 || stat != dataLength)
                {
                    Log.Debug("SPI spi_write(" + dataLength + ") return: " + stat);
                    throw new ApplicationException("SPI spi_write(" + dataLength + ") return: " + stat);
                }

                LogDebugData("SPI Master << ", dataReceiv, stat);

                return dataReceiv;
            }
        }

        byte[] ISpi.Query(byte[] data, ESpiSsSignal eSsSignal)
        {
            throw new NotImplementedException();
        }

        void ISpi.Delay(int value, ESpiSleepMode sleepMode)
        {
            throw new NotImplementedException();
        }

        void ISpi.SetBitRate(uint bitrateKhz)
        {
            if (!CheckBitRateValueValid(bitrateKhz))
                throw new ArgumentOutOfRangeException(nameof(bitrateKhz));

            CheckIfInitialized();
            lock (_instLock)
            {
                var stat = AardvarkWrapper.aa_spi_bitrate(AardvarkHandle, (int)bitrateKhz);
                if (stat != bitrateKhz)
                    throw new ApplicationException("SPI set spi_bitrate(" + bitrateKhz + ") return: " + stat);

                Log.Debug("SPI set bitrate Master " + bitrateKhz + "kHz done.");
            }
        }

        void ISpi.SlaveEnable()
        {
            CheckIfInitialized();
            lock (_instLock)
            {
                var status = -1;
                int i;
                for (i = 0; i < 2; i++)
                {
                    TapThread.Sleep(i * 100);

                    status = AardvarkWrapper.aa_spi_slave_enable(AardvarkHandle);
                    if (status == (int)AardvarkStatus.AA_OK)
                    {
                        Log.Debug("SPI SlaveEnable done with try " + (i + 1) + ".");
                        return;
                    }

                    Log.Debug("SPI SlaveEnable try " + (i + 1) + " return[" + status + "].");
                }

                throw new ApplicationException("SPI slave_enable return[" + status + "] with try " + i + ".");
            }
        }

        void ISpi.SlaveDisable()
        {
            CheckIfInitialized();
            lock (_instLock)
            {
                var status = -1;
                int i;
                for (i = 0; i < 2; i++)
                {
                    TapThread.Sleep(i * 100);

                    status = AardvarkWrapper.aa_spi_slave_disable(AardvarkHandle);
                    if (status == (int)AardvarkStatus.AA_OK)
                    {
                        Log.Debug("SPI SlaveDisable done with try " + (i + 1) + ".");
                        return;
                    }

                    Log.Debug("SPI SlaveDisable try " + (i + 1) + " return[" + status + "].");
                }

                throw new ApplicationException("SPI slave_disable return[" + status + "] with try " + i + ".");
            }
        }

        byte[] ISpi.SlaveRead(ushort numOfBytesMax, out int numOfBytesRead)
        {
            if (numOfBytesMax <= 0 || MaxTxRxBytes < numOfBytesMax)
                throw new ArgumentOutOfRangeException(nameof(numOfBytesMax),
                    "NumOfBytes must have positive value and not more than Maximum: " + MaxTxRxBytes);

            CheckIfInitialized();
            lock (_instLock)
            {
                var dataReceiv = new byte[numOfBytesMax];
                numOfBytesRead = AardvarkWrapper.aa_spi_slave_read(AardvarkHandle, numOfBytesMax, dataReceiv);
                if (numOfBytesRead < 0)
                    throw new ApplicationException("SPI spi_slave_read return: " + numOfBytesRead);

                LogDebugData("SPI Slave  << ", dataReceiv, numOfBytesRead);
                return dataReceiv;
            }
        }

        int ISpi.SlaveSetResponse(byte[] dataToResponse)
        {
            if (dataToResponse == null)
                throw new ArgumentNullException(nameof(dataToResponse));
            if (dataToResponse.Length < 1)
                throw new ArgumentException("Value cannot be an empty.", nameof(dataToResponse));

            CheckIfInitialized();
            lock (_instLock)
            {
                var stat = AardvarkWrapper.aa_spi_slave_set_response(AardvarkHandle,
                    Convert.ToByte(dataToResponse.Length), dataToResponse);
                LogDebugData("SPI Slave SetR", dataToResponse, stat);

                if (stat > 0)
                    return stat;

                throw new ApplicationException("SPI slave_set_response return: " + stat);
            }
        }

        #endregion

        #region Private Methods

        private bool CheckBitRateValueValid(uint bitRateKhz)
        {
            // The Aardvark adapter SPI master can operate at bitrates of 125 kHz, 250 Khz, 500 Khz, 1 Mhz, 2 MHz, 4 Mhz, and 8 Mhz.
            var aardvarkSpiMasterClockFreqsKhz = new List<int> { 125, 250, 500, 1000, 2000, 4000, 8000 };
            return aardvarkSpiMasterClockFreqsKhz.Any(freq => bitRateKhz == freq);
        }

        #endregion
    }
}