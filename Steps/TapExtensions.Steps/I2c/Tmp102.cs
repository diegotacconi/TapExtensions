using System;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c
{
    public class Tmp102
    {
        private readonly II2C _i2C;
        private readonly int _deviceAddress;

        public Tmp102(II2C i2C, int deviceAddress)
        {
            _i2C = i2C;
            _deviceAddress = deviceAddress;
        }

        public double ReadTemperature()
        {
            // var configReg = ReadConfigurationRegister();
            var temperatureReg = ReadTemperatureRegister();
            var msb = temperatureReg[0];
            var lsb = temperatureReg[1];
            var extendedMode = (lsb & 0b00000001) == 0b00000001;
            var temperature = ConvertToTemperature(msb, lsb, extendedMode);
            return temperature;
        }

        private byte[] ReadConfigurationRegister()
        {
            /*
             * Configuration Register (0x01)
             * +-------------------------------------------------------+-------------------------------------------------------+
             * |              Most Significant Byte (MSB)              |             Least Significant Byte (LSB)              |
             * +------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+
             * | D15  | D14  | D13  | D12  | D11  | D10  | D9   | D8   | D7   | D6   | D5   | D4   | D3   | D2   | D1   | D0   |
             * | OS   | R1   | R0   | F1   | F0   | POL  | TM   | SD   | CR1  | CR0  | AL   | EM   |  0   |  0   |  0   |  0   |
             * +------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+
             * One-Shot (OS)
             * Converter Resolution (R1/R0)
             * Fault Queue (F1/F0)
             * Polarity (POL)
             * Thermostat Mode (TM)
             * Shutdown Mode (SD)
             * Conversion Rate (CR1/CR0)
             * Alert (AL)
             * Extended Temperature Mode (EM)
             */
            var configReg = _i2C.Read((ushort)_deviceAddress, 2, new byte[] { 0x01 });
            return configReg;
        }

        private byte[] ReadTemperatureRegister()
        {
            /*
             * Temperature Register (0x00)
             *                 +-------------------------------------------------------+-------------------------------------------------------+
             *                 |              Most Significant Byte (MSB)              |             Least Significant Byte (LSB)              |
             * +---------------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+
             * |               | D15  | D14  | D13  | D12  | D11  | D10  | D9   | D8   | D7   | D6   | D5   | D4   | D3   | D2   | D1   | D0   |
             * |   Normal mode | T11  | T10  | T9   | T8   | T7   | T6   | T5   | T4   | T3   | T2   | T1   | T0   |  0   |  0   |  0   |  0   |
             * | Extended mode | T12  | T11  | T10  | T9   | T8   | T7   | T6   | T5   | T4   | T3   | T2   | T1   | T0   |  0   |  0   |  1   |
             * +---------------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+
             * Bit D0 of LSB indicates normal mode (EM bit = 0) or extended mode (EM bit = 1), and can be used to distinguish between the two
             * temperature register data formats.
             */
            var temperatureReg = _i2C.Read((ushort)_deviceAddress, 2, new byte[] { 0x00 });
            return temperatureReg;
        }

        private static double ConvertToTemperature(int msb, int lsb, bool extendedMode)
        {
            const double resolution = 0.0625;
            uint unsignedInt;
            if (extendedMode)
            {
                // Extended Temperature Range (13-bit)
                unsignedInt = (uint)((msb << 8) + lsb);
                unsignedInt >>= 3; //remove 3 lsb bits
            }
            else
            {
                // Normal Temperature Range (12-bit)
                unsignedInt = (uint)((msb << 8) + lsb);
                unsignedInt >>= 4; //remove 4 lsb bits
            }
            var signedInt = FromTwosComplement(unsignedInt, 12);
            var temperature = resolution * signedInt;
            return temperature;
        }

        private static int FromTwosComplement(uint unsignedInt, int bits)
        {
            if (bits < 1 || bits > 32)
                throw new ArgumentOutOfRangeException(nameof(bits),
                    "Parameter value out of range!");

            if (bits < 32 && unsignedInt > (1 << bits) - 1)
                throw new ArgumentOutOfRangeException(nameof(unsignedInt),
                    "Parameter value exceeds max value of given bit-length!");

            int signedInt;
            if ((unsignedInt & (1 << bits - 1)) != 0)
            {
                var complement = ~unsignedInt & ((1L << bits) - 1);
                // ReSharper disable once RedundantCast
                signedInt = -(int)((complement & ((1L << bits) - 1)) + 1);
            }
            else
            {
                signedInt = (int)unsignedInt;
            }
            return signedInt;
        }
    }
}