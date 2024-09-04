using OpenTap;

namespace TapExtensions.Interfaces.Spi
{
    #region enums

    /// <summary>
    ///     Mode0: Clock polarity 0 Clock phase 0. Data are captured on the clock's rising edge and data are output on falling edge <br />
    ///     Mode1: Clock polarity 0 Clock phase 1. Data are captured on the clock's falling edge and data are output on rising edge <br />
    ///     Mode2: Clock polarity 1 Clock phase 0. Data are captured on the clock's falling edge and data are output on rising edge <br />
    ///     Mode3: Clock polarity 1 Clock phase 1. Data are captured on the clock's rising edge and data are output on falling edge <br />
    /// </summary>
    public enum ESpiMode
    {
        Mode0 = 0,
        Mode1,
        Mode2,
        Mode3
    }

    /// <summary>
    ///     Msb: Most Significant Bit first <br />
    ///     Lsb: Least Significant Bit first <br />
    /// </summary>
    public enum ESpiBitOrder
    {
        Msb = 0,
        Lsb
    }

    public enum ESpiChipSelect
    {
        Device0 = 0,
        Device1
    }

    /// <summary>
    ///     Slave line is active low or high
    /// </summary>
    public enum ESpiChipSelectPolarity
    {
        ActiveLow = 0,
        ActiveHigh
    }

    /// <summary>
    ///     Mode0: SS1 is active low and SS2 is active low and SS3 is active low <br />
    ///     Mode1: SS1 is active high and SS2 is active low and SS3 is active low <br />
    ///     Mode2: SS1 is active low and SS2 is active high and SS3 is active low <br />
    ///     Mode3: SS1 is active high and SS2 is active high and SS3 is active low <br />
    ///     Mode4: SS1 is active low and SS2 is active low and SS3 is active high <br />
    ///     Mode5: SS1 is active high and SS2 is active low and SS3 is active high <br />
    ///     Mode6: SS1 is active low and SS2 is active high and SS3 is active high <br />
    ///     Mode7: SS1 is active high and SS2 is active high and SS3 is active high <br />
    /// </summary>
    public enum ESpiSsPolarity
    {
        Mode0 = 0x00,
        Mode1 = 0x01,
        Mode2 = 0x02,
        Mode3 = 0x03,
        Mode4 = 0x04,
        Mode5 = 0x05,
        Mode6 = 0x06,
        Mode7 = 0x07
    }

    public enum ESpiSleepMode
    {
        Cycle,
        Ns
    }

    /// <summary>
    ///     Mode0: SS1 is deasserted and SS2 is deasserted and SS3 is deasserted <br />
    ///     Mode1: SS1 is asserted and SS2 is deasserted and SS3 is deasserted <br />
    ///     Mode2: SS1 is deasserted and SS2 is asserted and SS3 is deasserted <br />
    ///     Mode3: SS1 is asserted and SS2 is asserted and SS3 is deasserted <br />
    ///     Mode4: SS1 is deasserted and SS2 is deasserted and SS3 is asserted <br />
    ///     Mode5: SS1 is asserted and SS2 is deasserted and SS3 is asserted <br />
    ///     Mode6: SS1 is deasserted and SS2 is asserted and SS3 is asserted <br />
    ///     Mode7: SS1 is asserted and SS2 is asserted and SS3 is asserted <br />
    /// </summary>
    public enum ESpiSsSignal
    {
        Mode0 = 0x00,
        Mode1 = 0x01,
        Mode2 = 0x02,
        Mode3 = 0x03,
        Mode4 = 0x04,
        Mode5 = 0x05,
        Mode6 = 0x06,
        Mode7 = 0x07
    }

    #endregion

    public interface ISpi : IInstrument
    {
        /// <summary>
        ///     Configures the SPI interface.
        /// </summary>
        /// <param name="mode">Mode0-3</param>
        /// <param name="bitOrder">Msb or Lsb</param>
        /// <param name="chipSelect">Device0 or Device1</param>
        /// <param name="chipSelectPolarity">Chip select polarity</param>
        /// <param name="bitRate">kHz</param>
        void Init(ESpiMode mode, ESpiBitOrder bitOrder, ESpiChipSelect chipSelect,
            ESpiChipSelectPolarity chipSelectPolarity, uint bitRate);

        /// <summary>
        ///     Configures the cheetah SPI interface
        /// </summary>
        /// <param name="mode">Mode0-3,select the polarity of the clock signal and the phase of the clock signal to sample on</param>
        /// <param name="bitOrder">Msb or Lsb</param>
        /// <param name="eSsPolarity">Mode0-7,a bit mask that indicates whether 3 SS line is active high or active low</param>
        void Init(ESpiMode mode, ESpiBitOrder bitOrder, ESpiSsPolarity eSsPolarity);

        /// <summary>
        ///     Query data from SPI device.
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <returns>Read data from spi device</returns>
        byte[] Query(byte[] data);

        /// <summary>
        ///     Query data from SPI device
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="eSsSignal">Mode0-7</param>
        /// <returns>Read data from spi device</returns>
        byte[] Query(byte[] data, ESpiSsSignal eSsSignal);

        /// <summary>
        ///     Check that bitrate is included in the list of device clock frequencies
        /// </summary>
        /// <param name="bitRatekHz">Value to be checked</param>
        /// <returns>Result of check. True = found / False = NOT found.</returns>
        bool CheckBitRateValueValid(uint bitRatekHz);

        /// <summary>
        ///     Queue a delay value on the bus
        /// </summary>
        /// <param name="value">
        ///     cycles of delay to add to the outbound shift if eSleepMode is Cycle,
        ///     or else amount of time for delay in nanoseconds if eSleepMode is Ns
        /// </param>
        /// <param name="sleepMode">Cycle/Ns</param>
        void Delay(int value, ESpiSleepMode sleepMode);

        /// <summary>
        ///     Sets SPI bus bit rate
        /// </summary>
        /// <param name="bitrateKhz">The requested bitrate in kHz</param>
        void SetBitRate(uint bitrateKhz);

        /// <summary>
        ///     Set the SPI-device to slave-mode
        /// </summary>
        void SlaveEnable();

        /// <summary>
        ///     Set the SPI-device to master-mode
        /// </summary>
        void SlaveDisable();

        /// <summary>
        ///     Read the bytes from an SPI slave reception
        /// </summary>
        /// <param name="numOfBytesMax">The maximum size of the data buffer</param>
        /// <param name="numOfBytesRead">The number of bytes read asynchronously</param>
        /// <returns>Received data</returns>
        byte[] SlaveRead(ushort numOfBytesMax, out int numOfBytesRead);

        /// <summary>
        ///     Set the slave response in the event the Aardvark adapter is put into slave mode and contacted by a master
        /// </summary>
        /// <param name="dataToResponse">The data to be set</param>
        /// <returns>Received data</returns>
        int SlaveSetResponse(byte[] dataToResponse);
    }
}