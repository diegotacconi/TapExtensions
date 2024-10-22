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

            var responseBytes = new List<byte>();
            for (var i = 0; i < numOfBytes; i++)
            {
                var dataAddress = (byte)(regAddress.First() + i);
                var command = $"sudo i2cget -y {I2CBus} 0x{slaveAddress:X2} 0x{dataAddress:X2}";
                SendSshQuery(command, 5, out var responseByte);

                if (string.IsNullOrWhiteSpace(responseByte))
                    throw new InvalidOperationException("No response");

                responseByte = responseByte.TrimEnd('\r', '\n');

                if (responseByte.ToUpper().StartsWith("0X"))
                    responseByte = responseByte.Substring(2, responseByte.Length - 2);

                if (!string.IsNullOrWhiteSpace(responseByte))
                    responseBytes.Add(Convert.ToByte(responseByte, 16));
            }

            return responseBytes.ToArray();
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
    }
}