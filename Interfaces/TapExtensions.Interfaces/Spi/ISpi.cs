using OpenTap;

namespace TapExtensions.Interfaces.Spi
{
    #region enums

    /// <summary>
    ///     Mode0: Clock polarity 0 Clock phase 0. Data is captured on the clock's rising edge and data is output on falling edge <br />
    ///     Mode1: Clock polarity 0 Clock phase 1. Data is captured on the clock's falling edge and data is output on rising edge <br />
    ///     Mode2: Clock polarity 1 Clock phase 0. Data is captured on the clock's falling edge and data is output on rising edge <br />
    ///     Mode3: Clock polarity 1 Clock phase 1. Data is captured on the clock's rising edge and data is output on falling edge <br />
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
        void Delay(int value, ESpiSleepMode sleepMode);

        void Init(ESpiMode mode, ESpiBitOrder bitOrder, ESpiChipSelect chipSelect,
            ESpiChipSelectPolarity chipSelectPolarity, uint bitRate);

        void Init(ESpiMode mode, ESpiBitOrder bitOrder, ESpiSsPolarity eSsPolarity);

        byte[] Query(byte[] data);

        byte[] Query(byte[] data, ESpiSsSignal eSsSignal);

        void SetBitRate(uint bitrateKhz);

        void SlaveDisable();

        void SlaveEnable();

        byte[] SlaveRead(ushort numOfBytesMax, out int numOfBytesRead);

        int SlaveSetResponse(byte[] dataToResponse);
    }
}