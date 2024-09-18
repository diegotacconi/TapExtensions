using System;
using System.Runtime.InteropServices;
using OpenTap;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Instruments.MultipleInterfaces.Aardvark
{
    public partial class Aardvark : II2C
    {
        #region I2C Interface Implementation

        byte[] II2C.Read(ushort slaveAddress, ushort numOfBytes)
        {
            lock (_instLock)
            {
                return I2CRead(slaveAddress, numOfBytes);
            }
        }

        byte[] II2C.Read(ushort slaveAddress, ushort numOfBytes, byte[] regAddress)
        {
            lock (_instLock)
            {
                I2CWrite(slaveAddress, regAddress, AardvarkI2cFlags.AA_I2C_NO_STOP);
                return I2CRead(slaveAddress, numOfBytes);
            }
        }

        void II2C.SetBitRate(uint bitRateKhz)
        {
            lock (_instLock)
            {
                CheckIfInitialized();
                Log.Debug($"Setting I2C bit rate to {bitRateKhz} kHz");
                var actualBitRateKhz = AardvarkWrapper.net_aa_i2c_bitrate(AardvarkHandle, (int)bitRateKhz);
                if (actualBitRateKhz != bitRateKhz)
                    throw new InvalidOperationException(
                        $"Error trying to set the I2C bit rate to {bitRateKhz} kHz. Actual bit rate was {actualBitRateKhz} kHz.");
            }
        }

        void II2C.SetBusTimeOutInMs(ushort timeOutMs)
        {
            lock (_instLock)
            {
                CheckIfInitialized();
                Log.Debug($"Setting I2C timeout to {timeOutMs} ms");
                var status = AardvarkWrapper.net_aa_i2c_bus_timeout(AardvarkHandle, timeOutMs);
                if (status != timeOutMs)
                    throw new InvalidOperationException(
                        $"Error trying to set the I2C bus timeout to {timeOutMs} ms.");
            }
        }

        void II2C.SlaveDisable()
        {
            lock (_instLock)
            {
                CheckIfInitialized();

                var status = -1;
                int i;
                for (i = 0; i < 2; i++)
                {
                    TapThread.Sleep(i * 100);

                    status = AardvarkWrapper.net_aa_i2c_slave_disable(AardvarkHandle);
                    if (status == (int)AardvarkStatus.AA_OK)
                    {
                        Log.Debug("I2C SlaveDisable done with try " + (i + 1) + ".");
                        return;
                    }

                    Log.Debug("I2C SlaveDisable try " + (i + 1) + " return[" + status + "].");
                }

                throw new InvalidOperationException("I2C SlaveDisable return[" + status + "] with try " + i + ".");
            }
        }

        void II2C.SlaveEnable(byte slaveAddress, ushort maxTxBytes, ushort maxRxBytes)
        {
            lock (_instLock)
            {
                CheckIfInitialized();

                if (slaveAddress <= 0)
                    throw new InvalidOperationException(
                        $"I2C: {nameof(slaveAddress)} must be greater than zero.");

                if (maxTxBytes <= 0 || MaxTxRxBytes < maxTxBytes)
                    throw new InvalidOperationException(
                        $"I2C: {nameof(maxTxBytes)} must have positive value and not more than {MaxTxRxBytes} bytes.");

                if (maxRxBytes <= 0 || MaxTxRxBytes < maxRxBytes)
                    throw new InvalidOperationException(
                        $"I2C: {nameof(maxRxBytes)} must have positive value and not more than {MaxTxRxBytes} bytes.");

                var status = -1;
                int i;
                for (i = 0; i < 2; i++)
                {
                    TapThread.Sleep(i * 100);
                    status = AardvarkWrapper.net_aa_i2c_slave_enable(AardvarkHandle, slaveAddress, maxTxBytes,
                        maxRxBytes);
                    if (status == (int)AardvarkStatus.AA_OK)
                    {
                        Log.Debug("I2C SlaveEnable(Add:" + slaveAddress + ") done with try " + (i + 1) + ".");
                        return;
                    }

                    Log.Debug("I2C SlaveEnable(Add:" + slaveAddress + ") try " + (i + 1) + " return[" + status + "].");
                }

                throw new InvalidOperationException(
                    "I2C SlaveEnable(Add:" + slaveAddress + ") return[" + status + "] with try " + i + ".");
            }
        }

        byte[] II2C.SlaveRead(byte slaveAddress, ushort numOfBytesMax, out int numOfBytesRead)
        {
            lock (_instLock)
            {
                CheckIfInitialized();

                if (slaveAddress <= 0)
                    throw new InvalidOperationException(
                        $"I2C: {nameof(slaveAddress)} must be greater than zero.");

                if (numOfBytesMax <= 0 || MaxTxRxBytes < numOfBytesMax)
                    throw new InvalidOperationException(
                        $"I2C: {nameof(numOfBytesMax)} must have positive value and not more than {MaxTxRxBytes} bytes.");

                var response = new byte[numOfBytesMax];
                numOfBytesRead =
                    AardvarkWrapper.net_aa_i2c_slave_read(AardvarkHandle, ref slaveAddress, numOfBytesMax, response);

                LogDebugData("I2C Slave  << ", response, numOfBytesRead);

                if (numOfBytesRead >= 0)
                    return response;

                throw new InvalidOperationException(
                    "I2C SlaveRead ERRor[" + numOfBytesRead + "] from addr:" + slaveAddress);
            }
        }

        void II2C.Write(ushort slaveAddress, ushort numOfBytes, byte[] command)
        {
            lock (_instLock)
            {
                I2CWrite(slaveAddress, command, AardvarkI2cFlags.AA_I2C_NO_FLAGS);
            }
        }

        void II2C.Write(ushort slaveAddress, byte[] regAddress, ushort numOfBytes, byte[] command)
        {
            lock (_instLock)
            {
                var regAddressLength = Convert.ToUInt16(regAddress.Length);
                var commandLength = Convert.ToUInt16(command.Length);
                var regAddressPlusCommand = new byte[regAddressLength + commandLength];

                for (var i = 0; i < regAddressLength; i++)
                    regAddressPlusCommand[i] = regAddress[i];

                for (int i = regAddressLength; i < regAddressLength + commandLength; i++)
                    regAddressPlusCommand[i] = command[i - regAddressLength];

                I2CWrite(slaveAddress, regAddressPlusCommand, AardvarkI2cFlags.AA_I2C_NO_FLAGS);
            }
        }

        #endregion

        #region Private Methods

        private byte[] I2CRead(ushort slaveAddress, ushort numOfBytes)
        {
            CheckIfInitialized();

            if (slaveAddress <= 0)
                throw new InvalidOperationException(
                    $"I2C: {nameof(slaveAddress)} must be greater than zero.");

            if (numOfBytes <= 0)
                throw new InvalidOperationException(
                    $"I2C: {nameof(numOfBytes)} must be greater than zero.");

            var response = new byte[numOfBytes];
            var status = AardvarkWrapper.net_aa_i2c_read(AardvarkHandle, slaveAddress,
                AardvarkI2cFlags.AA_I2C_NO_FLAGS, numOfBytes, response);

            if (status < 0)
                throw new InvalidOperationException(
                    $"I2C Read error: {status}");

            if (status == 0)
                throw new InvalidOperationException(
                    "I2C Read error: no bytes read");

            if (status != numOfBytes)
                throw new InvalidOperationException(
                    $"I2C Read error: read {status} bytes (expected {numOfBytes})");

            Log.Debug(string.Format("I2C (0x{0:X2}) << 0x{1}", slaveAddress,
                BitConverter.ToString(response).Replace("-", " 0x")));

            /*
            Log.Debug("Data read from device:");
            for (var i = 0; i < count; ++i)
            {
                if ((i & 0x0f) == 0)
                    Log.Debug("{0:x4}:  ", slaveAddress + i);

                Log.Debug("{0:x2} ", response[i] & 0xff);

                if (((i + 1) & 0x07) == 0)
                    Log.Debug(" ");
            }
            */

            return response;
        }

        private void I2CWrite(ushort slaveAddress, byte[] command, AardvarkI2cFlags flags)
        {
            CheckIfInitialized();

            if (slaveAddress <= 0)
                throw new InvalidOperationException(
                    $"I2C: {nameof(slaveAddress)} must be greater than zero.");

            if (command == null)
                throw new InvalidOperationException(
                    $"I2C: {nameof(command)} cannot be null.");

            var commandLength = Convert.ToUInt16(command.Length);

            var status = AardvarkWrapper.net_aa_i2c_write(AardvarkHandle, slaveAddress, flags, commandLength, command);

            if (status != commandLength)
            {
                Log.Warning("I2C Write error, retrying...");
                TapThread.Sleep(100);
                status = AardvarkWrapper.net_aa_i2c_write(AardvarkHandle, slaveAddress, flags, commandLength, command);
                if (status != commandLength)
                    throw new InvalidOperationException("I2C: Write failed!");
            }

            Log.Debug(string.Format("I2C (0x{0:X2}) >> 0x{1}", slaveAddress,
                BitConverter.ToString(command).Replace("-", " 0x")));
        }

        private void SetPullupResistors(EI2cPullup pullupMask)
        {
            lock (_instLock)
            {
                CheckIfInitialized();
                Log.Debug($"Setting I2C pull-up resistors to {pullupMask}");

                var status = AardvarkWrapper.net_aa_i2c_pullup(AardvarkHandle, (byte)pullupMask);
                if (status == (int)pullupMask)
                    return;

                var errorMsg = Marshal.PtrToStringAnsi(AardvarkWrapper.net_aa_status_string(status));
                throw new InvalidOperationException($"{Name}: Error {status}, {errorMsg}");
            }
        }

        #endregion
    }
}