using System;
using System.Collections.Generic;
using System.Linq;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Instruments.MultipleInterfaces.Raspi
{
    public partial class Raspi : II2C
    {
        #region I2C Interface Implementation

        private const ushort I2CBus = 1; // Available buses are: /dev/i2c-1, /dev/i2c-20, /dev/i2c-21.

        public byte[] Read(ushort slaveAddress, ushort numOfBytes)
        {
            throw new NotImplementedException();
        }

        public byte[] Read(ushort slaveAddress, ushort numOfBytes, byte[] regAddress)
        {
            /*
                Usage: i2cget [-f] [-y] [-a] I2CBUS CHIP-ADDRESS [DATA-ADDRESS [MODE [LENGTH]]]
                    I2CBUS is an integer or an I2C bus name
                    ADDRESS is an integer (0x08 - 0x77, or 0x00 - 0x7f if -a is given)
                    MODE is one of:
                        b (read byte data, default)
                        w (read word data)
                        c (write byte/read byte)
                        s (read SMBus block data)
                        i (read I2C block data)
                        Append p for SMBus PEC
                    LENGTH is the I2C block data length (between 1 and 32, default 32)

                Examples:
                    pi@lmi:~ $ i2cget -y 1 0x48 0x00 i 2
                    0x00 0x00
                    pi@lmi:~ $ i2cget -y 1 0x48 0x01 i 2
                    0x85 0x83
                    pi@lmi:~ $ i2cget -y 1 0x48 0x02 i 2
                    0x80 0x00
                    pi@lmi:~ $ i2cget -y 1 0x48 0x03 i 2
                    0x7f 0xff
             */

            if (slaveAddress <= 0)
                throw new InvalidOperationException(
                    $"{nameof(slaveAddress)} must be greater than zero.");

            if (numOfBytes < 1 || numOfBytes > 32)
                throw new InvalidOperationException(
                    $"{nameof(numOfBytes)} must be between 1 and 32.");

            if (regAddress == null)
                throw new InvalidOperationException(
                    $"{nameof(regAddress)} cannot be null.");

            var dataAddress = regAddress.First();
            var command = $"sudo i2cget -y {I2CBus} 0x{slaveAddress:X2} 0x{dataAddress:X2} i {numOfBytes}";
            SendSshQuery(command, 5, out var response);

            if (string.IsNullOrWhiteSpace(response))
                throw new InvalidOperationException("No response");

            return HexStringToBytes(response);
        }

        public void SetBitRate(uint bitRateKhz)
        {
            throw new NotImplementedException();
        }

        public void SetBusTimeOutInMs(ushort timeOutMs)
        {
            throw new NotImplementedException();
        }

        public void SlaveDisable()
        {
            throw new NotImplementedException();
        }

        public void SlaveEnable(byte slaveAddress, ushort maxTxBytes, ushort maxRxBytes)
        {
            throw new NotImplementedException();
        }

        public byte[] SlaveRead(byte slaveAddress, ushort numOfBytesMax, out int numOfBytesRead)
        {
            throw new NotImplementedException();
        }

        public void Write(ushort slaveAddress, byte[] command)
        {
            throw new NotImplementedException();
        }

        public void Write(ushort slaveAddress, byte[] regAddress, byte[] command)
        {
            throw new NotImplementedException();
        }

        #endregion

        private static byte[] HexStringToBytes(string hexValues)
        {
            var hexBytes = hexValues.Split(new[] { " ", "\r\n", "\n\r", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries);

            var bytes = new List<byte>();
            foreach (var hexByte in hexBytes)
            {
                var hex = hexByte;

                if (hex.ToUpper().StartsWith("0X"))
                    hex = hex.Substring(2, hex.Length - 2);

                if (!string.IsNullOrWhiteSpace(hex))
                    bytes.Add(Convert.ToByte(hex, 16));
            }

            if (!bytes.Any())
                throw new InvalidOperationException(
                    $"{nameof(HexStringToBytes)}: Cannot find any hexadecimal values to convert to bytes");

            return bytes.ToArray();
        }
    }
}