// Texas Instruments ADS1015 Precision ADC (Analog to Digital Converter)
// https://www.ti.com/product/ADS1015

using System;
using OpenTap;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c.Devices
{
    public class Ads1015
    {
        private readonly TraceSource _log = Log.CreateSource("Ads1015");
        private readonly II2C _i2CAdapter;
        private readonly ushort _deviceAddress;

        public Ads1015(II2C i2C, ushort deviceAddress = 0x48)
        {
            _i2CAdapter = i2C;
            _deviceAddress = deviceAddress;
        }

        #region Registers

        public (ushort conversion, ushort config, ushort loThresh, ushort hiThresh) ReadAllRegisters()
        {
            /*
            +------------------+---------------------------------------------------------+---------------------------------------------------------+---------------------------------+
            |     Register     | bit15  bit14  bit13  bit12  bit11  bit10  bit9   bit8   | bit7   bit6   bit5   bit4   bit3   bit2   bit1   bit0   |      Default (Reset) Value      |
            +------------------+---------------------------------------------------------+---------------------------------------------------------+---------------------------------+
            | 00b : Conversion |                                                         |                                                         | 0000 0000 0000 0000 = 0x00 0x00 |
            | 01b : Config     | OS     MUX2   MUX1   MUX0   PGA2   PGA1   PGA0   MODE   | DR2    DR1    DR2    C_MODE C_POL  C_PAT  C_QUE1 C_QUE0 | 1000 0101 1000 0011 = 0x85 0x83 |
            | 10b : Lo_thresh  |                                                         |                                                         | 1000 0000 0000 0000 = 0x80 0x00 |
            | 11b : Hi_thresh  |                                                         |                                                         | 0111 1111 1111 1111 = 0x7F 0xFF |
            +------------------+---------------------------------------------------------+---------------------------------------------------------+---------------------------------+

            Bit15, OS, Operational status or single-shot conversion start
                When writing:
                    0b : No effect
                    1b : Start a single conversion (when in power-down state)
                When reading:
                    0b : Device is currently performing a conversion
                    1b : Device is not currently performing a conversion

            Bits14:12, MUX[2:0], Input multiplexer configuration
                These bits configure the input multiplexer.
                    000b : AINP = AIN0 and AINN = AIN1 (default)
                    001b : AINP = AIN0 and AINN = AIN3
                    010b : AINP = AIN1 and AINN = AIN3
                    011b : AINP = AIN2 and AINN = AIN3
                    100b : AINP = AIN0 and AINN = GND
                    101b : AINP = AIN1 and AINN = GND
                    110b : AINP = AIN2 and AINN = GND
                    111b : AINP = AIN3 and AINN = GND
            */

            var conversion = ReadConversionRegister();
            var config = ReadConfigRegister();
            var loThresh = ReadLowerThresholdRegister();
            var hiThresh = ReadHigherThresholdRegister();

            _log.Debug("| Conversion       | Config           | Lower Threshold  | Higher Threshold |");
            _log.Debug($"| {ToBinaryString(conversion)} " +
                       $"| {ToBinaryString(config)} " +
                       $"| {ToBinaryString(loThresh)} " +
                       $"| {ToBinaryString(hiThresh)} |");

            return (conversion, config, loThresh, hiThresh);
        }

        private ushort ReadRegister(byte[] regAddress)
        {
            var register = _i2CAdapter.Read(_deviceAddress, 2, regAddress);
            return (ushort)((register[0] << 8) | register[1]);
        }

        private ushort ReadConversionRegister()
        {
            return ReadRegister(new byte[] { 0x00 });
        }

        private ushort ReadConfigRegister()
        {
            return ReadRegister(new byte[] { 0x01 });
        }

        private ushort ReadLowerThresholdRegister()
        {
            return ReadRegister(new byte[] { 0x02 });
        }

        private ushort ReadHigherThresholdRegister()
        {
            return ReadRegister(new byte[] { 0x03 });
        }

        #endregion

        #region Bit Manipulations

        private static string ToBinaryString(ushort number)
        {
            return Convert.ToString(number, 2).PadLeft(16, '0');
        }

        #endregion

        /*
        public double MeasureCurrent(byte adc)
        {
            // i2c_access -b /dev/i2c-12 -d 0x48 -S "1 0xd5 0x83"
            _i2C.Write((ushort)_deviceAddress, 3, new byte[] { 0x1, adc, 0x83 });

            // i2c_access -b /dev/i2c-12 -d 0x48 -G "2 0"
            var bytes = _i2C.Read((ushort)_deviceAddress, 2);

            var msb = bytes[0];
            var lsb = bytes[1];
            return ConvertToCurrent(msb, lsb);
        }
        */

        private static double ConvertToCurrent(int msb, int lsb)
        {
            var value = (uint)((msb << 4) + (lsb >> 4));
            const double gain = 1.63;
            var current = gain * value / 1000;
            return current;
        }
    }
}