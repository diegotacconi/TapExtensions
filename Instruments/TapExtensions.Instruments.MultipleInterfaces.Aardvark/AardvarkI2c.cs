﻿using System;
using OpenTap;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Instruments.MultipleInterfaces.Aardvark
{
    public partial class Aardvark : II2C
    {
        #region I2C Interface Implementation

        void II2C.Write(ushort slaveAddress, ushort numOfBytes, byte[] dataOut)
        {
            lock (_instLock)
            {
                CheckIfInitialized();
                if (numOfBytes <= 0)
                    throw new ApplicationException("Write: numOfBytes must have positive value.");
                if (dataOut == null)
                    throw new ApplicationException("Write: dataOut is null!");
                if (numOfBytes > dataOut.Length)
                    throw new ApplicationException("Write: numOfBytes is bigger than the length of dataOut!");

                var error = AardvarkWrapper.aa_i2c_write(AardvarkHandle, slaveAddress,
                    AardvarkI2cFlags.AA_I2C_NO_FLAGS, numOfBytes, dataOut);
                if (error != numOfBytes)
                {
                    Log.Debug("I2C Write error, retry..");
                    TapThread.Sleep(100);
                    error = AardvarkWrapper.aa_i2c_write(AardvarkHandle, slaveAddress,
                        AardvarkI2cFlags.AA_I2C_NO_FLAGS, numOfBytes, dataOut);
                    if (error != numOfBytes) throw new ApplicationException("I2C: Write failed!");
                }

                Log.Debug("I2C Write >> 0x" + slaveAddress.ToString("X2") + ", 0x" +
                          BitConverter.ToString(dataOut).Replace("-", " 0x"));
            }
        }

        void II2C.Write(ushort slaveAddress, byte[] registerAddress, ushort numOfBytes, byte[] dataOut)
        {
            lock (_instLock)
            {
                CheckIfInitialized();
                if (numOfBytes <= 0)
                    throw new ApplicationException("I2C: numOfBytes must have positive value.");
                if (dataOut == null)
                    throw new ApplicationException("I2C: dataOut is null!");
                if (numOfBytes > dataOut.Length)
                    throw new ApplicationException("I2C: numOfBytes is bigger than the length of dataOut!");

                var addrWidth = (ushort)registerAddress.Length;
                var dataToWrite = new byte[addrWidth + numOfBytes];
                for (var i = 0; i < addrWidth; i++) dataToWrite[i] = registerAddress[i];
                for (int i = addrWidth; i < addrWidth + numOfBytes; i++) dataToWrite[i] = dataOut[i - addrWidth];
                var numOfBytesToWrite = (ushort)(addrWidth + numOfBytes);

                var error = AardvarkWrapper.aa_i2c_write(AardvarkHandle, slaveAddress,
                    AardvarkI2cFlags.AA_I2C_NO_FLAGS, numOfBytesToWrite, dataToWrite);
                if (error != numOfBytesToWrite)
                {
                    Log.Debug("I2C Write error, retry..");
                    TapThread.Sleep(100);
                    error = AardvarkWrapper.aa_i2c_write(AardvarkHandle, slaveAddress,
                        AardvarkI2cFlags.AA_I2C_NO_FLAGS, numOfBytesToWrite, dataToWrite);
                    if (error != numOfBytesToWrite) throw new ApplicationException("I2C: Write failed!");
                }

                Log.Debug("I2C Write >> 0x" + slaveAddress.ToString("X2") + ", 0x" +
                          BitConverter.ToString(dataOut).Replace("-", " 0x"));
            }
        }

        byte[] II2C.Read(ushort slaveAddress, ushort numOfBytes)
        {
            lock (_instLock)
            {
                CheckIfInitialized();
                if (slaveAddress <= 0)
                    throw new ApplicationException("I2C: slaveAddress must have positive value.");
                if (numOfBytes <= 0)
                    throw new ApplicationException("I2C: numOfBytes must have positive value.");

                var dataIn = new byte[numOfBytes];
                var error = AardvarkWrapper.aa_i2c_read(AardvarkHandle, slaveAddress,
                    AardvarkI2cFlags.AA_I2C_NO_FLAGS, numOfBytes, dataIn);
                if (error != numOfBytes)
                    throw new ApplicationException("I2C: Read failed!");
                Log.Debug("I2C Read << 0x" + slaveAddress.ToString("X2") + ", 0x" +
                          BitConverter.ToString(dataIn).Replace("-", " 0x"));
                return dataIn;
            }
        }

        byte[] II2C.Read(ushort slaveAddress, ushort numOfBytes, byte[] regAddress)
        {
            var dataIn = new byte[numOfBytes];
            var addrWidth = (ushort)regAddress.Length;

            lock (_instLock)
            {
                CheckIfInitialized();
                if (slaveAddress <= 0)
                    throw new ApplicationException("I2C: slaveAddress must have positive value.");
                if (numOfBytes <= 0)
                    throw new ApplicationException("I2C: numOfBytes must have positive value.");

                var error = AardvarkWrapper.aa_i2c_write(AardvarkHandle, slaveAddress,
                    AardvarkI2cFlags.AA_I2C_NO_STOP,
                    addrWidth, regAddress);
                if (error != addrWidth)
                {
                    Log.Debug("I2C Read error, retry..");
                    TapThread.Sleep(100);
                    error = AardvarkWrapper.aa_i2c_write(AardvarkHandle, slaveAddress,
                        AardvarkI2cFlags.AA_I2C_NO_STOP,
                        addrWidth, regAddress);
                    if (error != addrWidth) throw new ApplicationException("I2C Write error!");
                }

                var count = AardvarkWrapper.aa_i2c_read(AardvarkHandle, slaveAddress,
                    AardvarkI2cFlags.AA_I2C_NO_FLAGS,
                    numOfBytes, dataIn);
                if (count < 0) throw new ApplicationException("I2C Read error: " + count);
                if (count == 0) throw new ApplicationException("I2C Read error: no bytes read");

                if (count != numOfBytes)
                    throw new ApplicationException("I2C Read error: read " + count + " bytes (expected " + numOfBytes +
                                                   ")");

                Log.Debug("Data read from device:");
                for (var i = 0; i < count; ++i)
                {
                    if ((i & 0x0f) == 0) Log.Debug("{0:x4}:  ", slaveAddress + i);
                    Log.Debug("{0:x2} ", dataIn[i] & 0xff);
                    if (((i + 1) & 0x07) == 0) Log.Debug(" ");
                }

                return dataIn;
            }
        }

        void II2C.SetBitRate(uint bitRateKhz)
        {
            lock (_instLock)
            {
                CheckIfInitialized();
                Log.Debug($"Setting I2C bit rate to {bitRateKhz} kHz");
                var actualBitRateKhz = AardvarkWrapper.aa_i2c_bitrate(AardvarkHandle, (int)bitRateKhz);
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
                Log.Debug("I2C: Setting timeout to " + timeOutMs + " ms");
                var returnval = AardvarkWrapper.aa_i2c_bus_timeout(AardvarkHandle, timeOutMs);
                if (returnval != timeOutMs)
                    throw new ApplicationException("I2C: Set bus timeout failed");
            }
        }

        void II2C.SlaveEnable(byte slaveAddress, ushort maxTxBytes, ushort maxRxBytes)
        {
            if (maxTxBytes <= 0 || MaxTxRxBytes < maxTxBytes)
                throw new ArgumentOutOfRangeException(nameof(maxTxBytes),
                    "Number of Tx bytes must have positive value and not more than Maximum: " + MaxTxRxBytes);
            if (maxRxBytes <= 0 || MaxTxRxBytes < maxRxBytes)
                throw new ArgumentOutOfRangeException(nameof(maxRxBytes),
                    "Number of Ex bytes must have positive value and not more than Maximum: " + MaxTxRxBytes);

            CheckIfInitialized();
            lock (_instLock)
            {
                var status = -1;
                int i;
                for (i = 0; i < 2; i++)
                {
                    TapThread.Sleep(i * 100);

                    status = AardvarkWrapper.aa_i2c_slave_enable(AardvarkHandle, slaveAddress, maxTxBytes,
                        maxRxBytes);
                    if (status == (int)AardvarkStatus.AA_OK)
                    {
                        Log.Debug("I2C SlaveEnable(Add:" + slaveAddress + ") done with try " + (i + 1) + ".");
                        return;
                    }

                    Log.Debug("I2C SlaveEnable(Add:" + slaveAddress + ") try " + (i + 1) + " return[" + status + "].");
                }

                throw new ApplicationException("I2C SlaveEnable(Add:" + slaveAddress + ") return[" + status +
                                               "] with try " + i + ".");
            }
        }

        void II2C.SlaveDisable()
        {
            CheckIfInitialized();
            lock (_instLock)
            {
                var status = -1;
                int i;
                for (i = 0; i < 2; i++)
                {
                    TapThread.Sleep(i * 100);

                    status = AardvarkWrapper.aa_i2c_slave_disable(AardvarkHandle);
                    if (status == (int)AardvarkStatus.AA_OK)
                    {
                        Log.Debug("I2C SlaveDisable done with try " + (i + 1) + ".");
                        return;
                    }

                    Log.Debug("I2C SlaveDisable try " + (i + 1) + " return[" + status + "].");
                }

                throw new ApplicationException("I2C SlaveDisable return[" + status + "] with try " + i + ".");
            }
        }

        byte[] II2C.SlaveRead(byte slaveAddress, ushort numOfBytesMax, out int numOfBytesRead)
        {
            if (slaveAddress <= 0)
                throw new ArgumentOutOfRangeException(nameof(slaveAddress),
                    "I2C slaveAddress must have positive value.");
            if (numOfBytesMax <= 0 || MaxTxRxBytes < numOfBytesMax)
                throw new ArgumentOutOfRangeException(nameof(numOfBytesMax),
                    "Number of bytes must have positive value and not more than Maximum: " + MaxTxRxBytes);

            CheckIfInitialized();
            lock (_instLock)
            {
                var data = new byte[numOfBytesMax];
                numOfBytesRead =
                    AardvarkWrapper.aa_i2c_slave_read(AardvarkHandle, ref slaveAddress, numOfBytesMax, data);
                LogDebugData("I2C Slave  << ", data, numOfBytesRead);

                if (numOfBytesRead >= 0)
                    return data;

                Log.Debug("I2C slave_read ERRor[" + numOfBytesRead + "] from addr:" + slaveAddress);
                throw new ApplicationException("I2C slave_read ERRor[" + numOfBytesRead + "] from addr:" +
                                               slaveAddress);
            }
        }

        #endregion

        #region Private Methods

        private void SetPullupResistors(EPullupResistors state)
        {
            lock (_instLock)
            {
                CheckIfInitialized();
                Log.Debug($"Setting I2C pull-up resistors to {state}");
                int status;
                switch (state)
                {
                    case EPullupResistors.Off:
                        status = AardvarkWrapper.aa_i2c_pullup(AardvarkHandle, 0x00);
                        if (status == 0x00) return;
                        break;
                    case EPullupResistors.On:
                        status = AardvarkWrapper.aa_i2c_pullup(AardvarkHandle, 0x03);
                        if (status == 0x03) return;
                        break;
                    default:
                        throw new ArgumentException(
                            $"{Name}: Case not found for {nameof(state)}={state}");
                }

                var errorMsg = AardvarkWrapper.aa_status_string(status);
                throw new InvalidOperationException($"{Name}: Error {status}, {errorMsg}");
            }
        }

        #endregion
    }
}