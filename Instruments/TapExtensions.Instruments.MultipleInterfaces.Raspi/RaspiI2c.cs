/*
   Usage:
     i2cget [-f] [-y] [-a] I2CBUS CHIP-ADDRESS [DATA-ADDRESS [MODE [LENGTH]]]

   Examples:
     $ sudo i2cget -y 1 0x48 0x01 i 2
     0x85 0x83

   Usage:
     i2cset [-f] [-y] [-m MASK] [-r] [-a] I2CBUS CHIP-ADDRESS DATA-ADDRESS [VALUE] ... [MODE]

   Examples:
     $ sudo i2cset -y 1 0x48 0x01 0xc5 0x83 i
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var sshCommand = $"sudo i2cget -y {I2CBus} 0x{slaveAddress:X2} 0x{dataAddress:X2} i {numOfBytes}";
            SendSshQuery(sshCommand, 5, out var response);

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
            if (slaveAddress <= 0)
                throw new InvalidOperationException(
                    $"{nameof(slaveAddress)} must be greater than zero.");

            if (regAddress == null)
                throw new InvalidOperationException(
                    $"{nameof(regAddress)} cannot be null.");

            if (command == null)
                throw new InvalidOperationException(
                    $"{nameof(command)} cannot be null.");

            var regAddressLength = Convert.ToUInt16(regAddress.Length);
            var commandLength = Convert.ToUInt16(command.Length);
            var regAddressPlusCommand = new byte[regAddressLength + commandLength];

            for (var i = 0; i < regAddressLength; i++)
                regAddressPlusCommand[i] = regAddress[i];

            for (int i = regAddressLength; i < regAddressLength + commandLength; i++)
                regAddressPlusCommand[i] = command[i - regAddressLength];

            var sshCommand = $"sudo i2cset -y {I2CBus} 0x{slaveAddress:X2} {BytesToHexString(regAddressPlusCommand)} i";
            SendSshQuery(sshCommand, 5, out _);
        }

        #endregion

        #region Private Methods

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

        private static string BytesToHexString(byte[] bytes)
        {
            var hex = new StringBuilder();
            foreach (var b in bytes)
                hex.AppendFormat("0x{0:X2} ", b);

            return hex.ToString().Trim();
        }

        #endregion
    }
}