using System;
using System.Collections.Generic;
using System.Linq;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Instruments.MultipleInterfaces.Raspi
{
    public partial class Raspi : II2C
    {
        #region I2C Interface Implementation

        private const ushort I2CBus = 1;

        public byte[] Read(ushort slaveAddress, ushort numOfBytes)
        {
            throw new NotImplementedException();
        }

        public byte[] Read(ushort slaveAddress, ushort numOfBytes, byte[] regAddress)
        {
            if (slaveAddress <= 0)
                throw new InvalidOperationException(
                    $"{nameof(slaveAddress)} must be greater than zero.");

            if (numOfBytes <= 0)
                throw new InvalidOperationException(
                    $"{nameof(numOfBytes)} must be greater than zero.");

            if (regAddress == null)
                throw new InvalidOperationException(
                    $"{nameof(regAddress)} cannot be null.");

            CheckIfConnected();

            var chipAddress = $"0x{slaveAddress:X2}";
            var dataAddress = $"0x{regAddress.First():X2}";
            var command = $"sudo i2cget -y {I2CBus} {chipAddress} {dataAddress} i {numOfBytes}";
            SendSshQuery(command, 5, out var response);

            if (string.IsNullOrWhiteSpace(response))
                throw new InvalidOperationException("No response");

            var responseBytes = HexStringToBytes(response);
            var responseLength = responseBytes.Length;
            if (responseLength != numOfBytes)
                throw new InvalidOperationException(
                    $"Response's length of {responseLength} does not match requested {nameof(numOfBytes)} of {numOfBytes}");

            // Log.Debug(string.Format("I2C (0x{0:X2}) << 0x{1}", slaveAddress,
            //     BitConverter.ToString(responseBytes).Replace("-", " 0x")));

            return responseBytes;
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

        #endregion
    }
}