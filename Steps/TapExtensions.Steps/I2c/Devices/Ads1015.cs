// Texas Instruments ADS1015 Precision ADC (Analog to Digital Converter)
// https://www.ti.com/product/ADS1015

using System;
using System.Collections.Generic;
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

        #region Documentation

        /*
            +------------------+---------------------------------------------------------+---------------------------------------------------------+---------------------------------+
            |     Register     | bit15  bit14  bit13  bit12  bit11  bit10  bit9   bit8   | bit7   bit6   bit5   bit4   bit3   bit2   bit1   bit0   |      Default (Reset) Value      |
            +------------------+---------------------------------------------------------+---------------------------------------------------------+---------------------------------+
            | 00b : Conversion |                                                         |                                                         | 0000 0000 0000 0000 = 0x00 0x00 |
            | 01b : Config     | OS     MUX2   MUX1   MUX0   PGA2   PGA1   PGA0   MODE   | DR2    DR1    DR2    C_MODE C_POL  C_PAT  C_QUE1 C_QUE0 | 1000 0101 1000 0011 = 0x85 0x83 |
            | 10b : Lo_thresh  |                                                         |                                                         | 1000 0000 0000 0000 = 0x80 0x00 |
            | 11b : Hi_thresh  |                                                         |                                                         | 0111 1111 1111 1111 = 0x7F 0xFF |
            +------------------+---------------------------------------------------------+---------------------------------------------------------+---------------------------------+
        */

        #endregion

        #region Registers

        public (ushort conversion, ushort config, ushort loThresh, ushort hiThresh) ReadAllRegisters()
        {
            var conversion = ReadConversionRegister();
            var config = ReadConfigRegister();
            var loThresh = ReadLowerThresholdRegister();
            var hiThresh = ReadHigherThresholdRegister();

            _log.Debug("| Conversion       | Config           | Lower Threshold  | Higher Threshold |");
            _log.Debug($"| {BinaryToString(conversion)} " +
                       $"| {BinaryToString(config)} " +
                       $"| {BinaryToString(loThresh)} " +
                       $"| {BinaryToString(hiThresh)} |");

            return (conversion, config, loThresh, hiThresh);
        }

        private ushort ReadRegister(byte[] regAddress)
        {
            var regValue = _i2CAdapter.Read(_deviceAddress, 2, regAddress);
            return (ushort)((regValue[0] << 8) | regValue[1]);
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

        private void WriteRegister(byte[] regAddress, ushort regValue)
        {
            var bytes = BitConverter.GetBytes(regValue);
            Array.Reverse(bytes, 0, bytes.Length);
            _i2CAdapter.Write(_deviceAddress, regAddress, bytes);
        }

        private void WriteConfigRegister(ushort regValue)
        {
            WriteRegister(new byte[] { 0x01 }, regValue);
        }

        #endregion

        /// <summary> Programmable Gain Amplifier (PGA) with input ranges from ±256mV to ±6.144V </summary>
        public enum EGainPrecision
        {
            [Display("Range = \u00b16.144V and LSB Size = 3mV")]
            Range0,

            [Display("Range = \u00b14.096V and LSB Size = 2mV")]
            Range1,

            [Display("Range = \u00b12.048V and LSB Size = 1mV")]
            Range2,

            [Display("Range = \u00b11.024V and LSB Size = 0.5mV")]
            Range3,

            [Display("Range = \u00b10.512V and LSB Size = 0.25mV")]
            Range4,

            [Display("Range = \u00b10.256V and LSB Size = 0.125mV")]
            Range5
        }

        /// <summary> Input multiplexer (MUX) </summary>
        public enum EInputMux
        {
            [Display("AINp = AIN0 and AINn = GND")]
            Ain0,

            [Display("AINp = AIN1 and AINn = GND")]
            Ain1,

            [Display("AINp = AIN2 and AINn = GND")]
            Ain2,

            [Display("AINp = AIN3 and AINn = GND")]
            Ain3
        }

        internal static (ushort gainBits, double gainLsbSize) GetGainBits(EGainPrecision gainPrecisionSelection)
        {
            var gainDictionary = new Dictionary<EGainPrecision, (ushort gainBits, double gainLsbSize)>
            {
                { EGainPrecision.Range0, (0b0000_0000_0000_0000, 3) },
                { EGainPrecision.Range1, (0b0000_0010_0000_0000, 2) },
                { EGainPrecision.Range2, (0b0000_0100_0000_0000, 1) },
                { EGainPrecision.Range3, (0b0000_0110_0000_0000, 0.5) },
                { EGainPrecision.Range4, (0b0000_1000_0000_0000, 0.25) },
                { EGainPrecision.Range5, (0b0000_1010_0000_0000, 0.125) }
            };

            if (!gainDictionary.TryGetValue(gainPrecisionSelection, out var gainTuple))
                throw new ArgumentException(
                    $"{nameof(gainDictionary)} does not have an entry for {nameof(gainPrecisionSelection)}={gainPrecisionSelection}.");

            return gainTuple;
        }

        internal static ushort GetInputBits(EInputMux inputMuxSelection)
        {
            var inputDictionary = new Dictionary<EInputMux, ushort>
            {
                { EInputMux.Ain0, 0b0100_0000_0000_0000 },
                { EInputMux.Ain1, 0b0101_0000_0000_0000 },
                { EInputMux.Ain2, 0b0110_0000_0000_0000 },
                { EInputMux.Ain3, 0b0111_0000_0000_0000 }
            };

            if (!inputDictionary.TryGetValue(inputMuxSelection, out var inputBits))
                throw new ArgumentException(
                    $"{nameof(inputDictionary)} does not have an entry for {nameof(inputMuxSelection)}={inputMuxSelection}.");

            return inputBits;
        }

        public double ConfigAndMeasure(EInputMux inputMux, EGainPrecision gainPrecision)
        {
            // Config
            var (gainBits, gainLsbSize) = GetGainBits(gainPrecision);
            var inputBits = GetInputBits(inputMux);
            const ushort commonBits = 0b1000_0001_1000_0011;
            var regValue = (ushort)(commonBits | inputBits | gainBits);
            WriteConfigRegister(regValue);
            _log.Debug($" config      = {BinaryToString(regValue)}");

            // Measure
            var conversion = ReadConversionRegister();
            _log.Debug($" conversion  = {BinaryToString(conversion)}");

            // Remove 4 bits (to get actual value)
            var value = (ushort)(conversion >> 4);
            _log.Debug($" value       = {BinaryToString(value)}");

            // Convert to Voltage
            var voltage = gainLsbSize * value / 1000;
            _log.Debug($" voltage     = {voltage}");

            return voltage;
        }

        private static string BinaryToString(ushort number)
        {
            return Convert.ToString(number, 2).PadLeft(16, '0');
        }

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