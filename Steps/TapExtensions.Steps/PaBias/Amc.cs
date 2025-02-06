// Texas Instruments Bias IC
// https://www.ti.com/amc

using System;
using System.Collections.Generic;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.PaBias
{
    public class Amc
    {
        private readonly II2C _i2CAdapter;
        private readonly ushort _deviceAddress;
        private static readonly byte[] Page1 = { 0x7E, 0x01 };

        public Amc(II2C i2CAdapter, ushort deviceAddress)
        {
            _i2CAdapter = i2CAdapter;
            _deviceAddress = deviceAddress;
        }

        public void SetPage(byte[] page)
        {
            _i2CAdapter.Write(_deviceAddress, page);
        }

        public List<string> ReadAll()
        {
            var lines = new List<string>();

            const int bus = 0;
            var page = Page1[1];

            // get config page
            SetPage(Page1);
            for (byte reg = 0; reg <= 41; reg++)
            {
                // Get reg value
                var res = _i2CAdapter.Read(_deviceAddress, 1, new[] { reg })[0];
                lines.Add($"{bus},0x{_deviceAddress:X2},{page},0x{reg:X2},0x{res:X2}");
            }

            /*
            # get clamp values
            page=2
            $(i2c_access -b /dev/i2c-$bus -d $addr -S "0x7E $page")
            for reg in `seq 16 1 19`;do
                # get reg value
                res=$(i2c_access -b /dev/i2c-$bus -d $addr -G "1 $reg")
                #echo $bus,$addr,$page,$reg,$res
                #printf '0x%02X,0x%02X\n' "$reg" $res
                printf '%02d,0x%02X,%01d,0x%02X,0x%02X\n' "$bus" $addr "$page" "$reg" $res

            done
               
            # get LUTS and base values

            # lutdis
            $(i2c_access -b /dev/i2c-$bus -d $addr -S "0x7E 0x01")
            $(i2c_access -b /dev/i2c-$bus -d $addr -S "0x0A 0x11")

            for page in `seq 4 1 5`;do
                $(i2c_access -b /dev/i2c-$bus -d $addr -S "0x7E $page")
                for reg in `seq 1 2 107`;do
                    # get reg value
                    res=$(i2c_access -b /dev/i2c-$bus -d $addr -G "1 $reg")
                    #echo $bus,$addr,$page,$reg,$res
                    printf '%02d,0x%02X,%01d,0x%02X,0x%02X\n' "$bus" $addr "$page" "$reg" $res
                done
            done

            # lut enable
            $(i2c_access -b /dev/i2c-$bus -d $addr -S "0x7E 0x01")
            $(i2c_access -b /dev/i2c-$bus -d $addr -S "0x0A 0x01")
             */

            return lines;
        }

        public double MeasureTemperature()
        {
            SetPage(Page1);

            var extTempRange = false;
            byte[] highByte = { 0x0 };
            byte[] lowByte = { 0x1 };

            // Get Configuration Register 1 LSB from Page 1 of AMC7904
            var configReg1LowByte = _i2CAdapter.Read(_deviceAddress, 1, new byte[] { 0x08 });
            var highBytes = _i2CAdapter.Read(_deviceAddress, 1, highByte);
            var lowBytes = _i2CAdapter.Read(_deviceAddress, 1, lowByte);

            // Check if TMPRANGE is 'Set', i.e. Bit 2 of Configuration Register 1 LSB is '1'
            //  1 = -64C to +191C (Extended Range)
            //  0 = -40C to +127C
            if ((configReg1LowByte[0] & 4) == 4)
                extTempRange = true;

            return ConvertToTemperature(highBytes[0], lowBytes[0], extTempRange);
        }

        private static double ConvertToTemperature(int tempHighByte, int tempLowByte, bool extTempRange)
        {
            const double dScaler = 0.0625;
            double temperature;
            if (extTempRange)
            {
                // Extended Range Calculation
                var intValue = tempHighByte - 64;
                var decValue = (tempLowByte >> 4) * dScaler;
                temperature = intValue + decValue; // Combine the two bytes
            }
            else
            {
                var smallInt = (uint)((tempHighByte << 8) + tempLowByte);
                smallInt >>= 4; // Remove 4 lsb bits
                var nativeInt = FromTwosComplement(smallInt, 12);
                temperature = dScaler * nativeInt;
            }

            return temperature;
        }

        private static int FromTwosComplement(uint twosComplement, int bits)
        {
            if (bits < 1 || bits > 32)
                throw new ArgumentOutOfRangeException(nameof(bits),
                    "Parameter value out of range!");
            if (bits < 32 && twosComplement > (1 << bits) - 1)
                throw new ArgumentOutOfRangeException(nameof(twosComplement),
                    "Parameter value exceeds max value of given bit-length!");

            int nativeInt;
            if ((twosComplement & (1 << (bits - 1))) != 0)
            {
                var complement = ~twosComplement & ((1L << bits) - 1);
                // ReSharper disable once RedundantCast
                nativeInt = -(int)((complement & ((1L << bits) - 1)) + 1);
            }
            else
            {
                nativeInt = (int)twosComplement;
            }

            return nativeInt;
        }
    }
}