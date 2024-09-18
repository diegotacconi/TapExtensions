﻿using System;
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
                CheckIfInitialized();

                if (slaveAddress <= 0)
                    throw new InvalidOperationException(
                        "I2C: slaveAddress must have positive value.");

                if (numOfBytes <= 0)
                    throw new InvalidOperationException(
                        "I2C: cmdLength must have positive value.");

                var response = new byte[numOfBytes];
                var error = AardvarkWrapper.net_aa_i2c_read(AardvarkHandle, slaveAddress,
                    AardvarkI2cFlags.AA_I2C_NO_FLAGS, numOfBytes, response);

                if (error != numOfBytes)
                    throw new InvalidOperationException("I2C: Read failed!");

                Log.Debug("I2C Read << 0x" + slaveAddress.ToString("X2") + ", 0x" +
                          BitConverter.ToString(response).Replace("-", " 0x"));
                return response;
            }
        }

        byte[] II2C.Read(ushort slaveAddress, ushort numOfBytes, byte[] regAddress)
        {
            var response = new byte[numOfBytes];
            var regAddressLength = (ushort)regAddress.Length;

            lock (_instLock)
            {
                CheckIfInitialized();

                if (slaveAddress <= 0)
                    throw new InvalidOperationException(
                        "I2C: slaveAddress must have positive value.");

                if (numOfBytes <= 0)
                    throw new InvalidOperationException(
                        "I2C: cmdLength must have positive value.");

                Log.Debug(string.Format("I2C (0x{0:X2}) >> 0x{1}", slaveAddress,
                    BitConverter.ToString(regAddress).Replace("-", " 0x")));

                var error = AardvarkWrapper.net_aa_i2c_write(AardvarkHandle, slaveAddress,
                    AardvarkI2cFlags.AA_I2C_NO_STOP, regAddressLength, regAddress);
                if (error != regAddressLength)
                {
                    Log.Warning("I2C Write error, retry...");
                    TapThread.Sleep(100);
                    error = AardvarkWrapper.net_aa_i2c_write(AardvarkHandle, slaveAddress,
                        AardvarkI2cFlags.AA_I2C_NO_STOP, regAddressLength, regAddress);
                    if (error != regAddressLength)
                        throw new InvalidOperationException("I2C Write error!");
                }

                var count = AardvarkWrapper.net_aa_i2c_read(AardvarkHandle, slaveAddress,
                    AardvarkI2cFlags.AA_I2C_NO_FLAGS, numOfBytes, response);

                if (count < 0)
                    throw new InvalidOperationException(
                        $"I2C Read error: {count}");

                if (count == 0)
                    throw new InvalidOperationException(
                        "I2C Read error: no bytes read");

                if (count != numOfBytes)
                    throw new InvalidOperationException(
                        $"I2C Read error: read {count} bytes (expected {numOfBytes})");

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
                var actualTimeOutMs = AardvarkWrapper.net_aa_i2c_bus_timeout(AardvarkHandle, timeOutMs);
                if (actualTimeOutMs != timeOutMs)
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
                        "I2C: slaveAddress must have positive value.");

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

        void II2C.Write(ushort slaveAddress, ushort cmdLength, byte[] command)
        {
            lock (_instLock)
            {
                CheckIfInitialized();

                if (slaveAddress <= 0)
                    throw new InvalidOperationException(
                        "I2C: slaveAddress must have positive value.");

                if (cmdLength <= 0)
                    throw new InvalidOperationException(
                        "I2C: cmdLength must have positive value.");

                if (command == null)
                    throw new InvalidOperationException(
                        "I2C: command is null!");

                if (cmdLength > command.Length)
                    throw new InvalidOperationException(
                        "I2C: cmdLength is bigger than the length of command!");

                var error = AardvarkWrapper.net_aa_i2c_write(AardvarkHandle, slaveAddress,
                    AardvarkI2cFlags.AA_I2C_NO_FLAGS, cmdLength, command);

                if (error != cmdLength)
                {
                    Log.Warning("I2C Write error, retry...");
                    TapThread.Sleep(100);
                    error = AardvarkWrapper.net_aa_i2c_write(AardvarkHandle, slaveAddress,
                        AardvarkI2cFlags.AA_I2C_NO_FLAGS, cmdLength, command);
                    if (error != cmdLength)
                        throw new InvalidOperationException("I2C: Write failed!");
                }

                Log.Debug("I2C Write >> 0x" + slaveAddress.ToString("X2") + ", 0x" +
                          BitConverter.ToString(command).Replace("-", " 0x"));
            }
        }

        void II2C.Write(ushort slaveAddress, byte[] regAddress, ushort cmdLength, byte[] command)
        {
            lock (_instLock)
            {
                CheckIfInitialized();

                if (slaveAddress <= 0)
                    throw new InvalidOperationException(
                        "I2C: slaveAddress must have positive value.");

                if (cmdLength <= 0)
                    throw new InvalidOperationException(
                        "I2C: cmdLength must have positive value.");

                if (command == null)
                    throw new InvalidOperationException(
                        "I2C: command is null!");

                if (cmdLength > command.Length)
                    throw new InvalidOperationException(
                        "I2C: cmdLength is bigger than the length of command!");

                var regAddressLength = (ushort)regAddress.Length;
                var dataToWrite = new byte[regAddressLength + cmdLength];
                for (var i = 0; i < regAddressLength; i++) dataToWrite[i] = regAddress[i];
                for (int i = regAddressLength; i < regAddressLength + cmdLength; i++)
                    dataToWrite[i] = command[i - regAddressLength];
                var numOfBytesToWrite = (ushort)(regAddressLength + cmdLength);

                var error = AardvarkWrapper.net_aa_i2c_write(AardvarkHandle, slaveAddress,
                    AardvarkI2cFlags.AA_I2C_NO_FLAGS, numOfBytesToWrite, dataToWrite);

                if (error != numOfBytesToWrite)
                {
                    Log.Warning("I2C Write error, retry...");
                    TapThread.Sleep(100);
                    error = AardvarkWrapper.net_aa_i2c_write(AardvarkHandle, slaveAddress,
                        AardvarkI2cFlags.AA_I2C_NO_FLAGS, numOfBytesToWrite, dataToWrite);
                    if (error != numOfBytesToWrite)
                        throw new InvalidOperationException("I2C: Write failed!");
                }

                Log.Debug("I2C Write >> 0x" + slaveAddress.ToString("X2") + ", 0x" +
                          BitConverter.ToString(command).Replace("-", " 0x"));
            }
        }

        #endregion

        #region Private Methods

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