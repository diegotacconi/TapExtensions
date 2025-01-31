// Texas Instruments TCA6416A I/O Expander
// https://www.ti.com/product/TCA6416A
//
// Provides 16-bits of general purpose parallel input/output (I/O) expansion.
// Can operate with a power supply voltage ranging from 1.65V to 5.5V.
// Supports both 100kHz (Standard-mode) and 400kHz(Fast-mode) clock frequencies.
// Has eight 8-bit data registers:
// - Two Configuration registers (input or output selection),
// - Two Input Port registers,
// - Two Output Port registers,
// - Two Polarity Inversion registers.
// At power on or after a reset, the I/Os are configured as inputs.

using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c.Devices
{
    // ReSharper disable InconsistentNaming
    public enum ETca6416Pin
    {
        // Enum's number is the bit position index, as in 2 to the power of the bit position index.
        // Examples: 2^0 = 1, 2^1 = 2, ... , 2^15 = 32768.
        P00_Pin04 = 0,
        P01_Pin05 = 1,
        P02_Pin06 = 2,
        P03_Pin07 = 3,
        P04_Pin08 = 4,
        P05_Pin09 = 5,
        P06_Pin10 = 6,
        P07_Pin11 = 7,
        P10_Pin13 = 8,
        P11_Pin14 = 9,
        P12_Pin15 = 10,
        P13_Pin16 = 11,
        P14_Pin17 = 12,
        P15_Pin18 = 13,
        P16_Pin19 = 14,
        P17_Pin20 = 15
    }

    public class Tca6416A : IGpioDevice
    {
        private readonly TraceSource log = Log.CreateSource("Tca6416A");
        private readonly II2C _i2CAdapter;
        private readonly ushort _deviceAddress;

        public Tca6416A(II2C i2CAdapter, ushort deviceAddress = 0x20)
        {
            _i2CAdapter = i2CAdapter;
            _deviceAddress = deviceAddress;
        }

        #region Registers

        public void ReadAllRegisters()
        {
            /*
             * The Input Port registers (registers 0 and 1) reflect the incoming logic levels of the pins,
             * regardless of whether the pin is defined as an input or an output by the Configuration register.
             * They act only on read operation. Writes to these registers have no effect.
             * +-------------------------------------------------------+-------------------------------------------------------+
             * |             Register 0x00 = Input Port 0              |             Register 0x01 = Input Port 1              |
             * +------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+
             * | I-07 | I-06 | I-05 | I-04 | I-03 | I-02 | I-01 | I-00 | I-17 | I-16 | I-15 | I-14 | I-13 | I-12 | I-11 | I-10 |
             * +------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+
             *
             *
             * The Output Port registers (registers 2 and 3) shows the outgoing logic levels of the pins defined as outputs by
             * the Configuration register. Bit values in these registers have no effect on pins defined as inputs. In turn, reads
             * from these registers reflect the value that is in the flip-flop controlling the output selection, not the actual
             * pin value.
             * +-------------------------------------------------------+-------------------------------------------------------+
             * |             Register 0x02 = Output Port 0             |             Register 0x03 = Output Port 1             |
             * +------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+
             * | O-07 | O-06 | O-05 | O-04 | O-03 | O-02 | O-01 | O-00 | O-17 | O-16 | O-15 | O-14 | O-13 | O-12 | O-11 | O-10 |
             * +------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+
             *
             *
             * The Polarity Inversion registers (register 4 and 5) allow polarity inversion of pins defined as inputs by the
             * Configuration register. If a bit in these registers is set (written with 1), the corresponding port pin's polarity
             * is inverted. If a bit in these registers is cleared (written with a 0), the corresponding port pin's original
             * polarity is retained.
             * +-------------------------------------------------------+-------------------------------------------------------+
             * |             Register 0x04 = Polarity Inversion 0      |             Register 0x05 = Polarity Inversion 1      |
             * +------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+
             * | P-07 | P-06 | P-05 | P-04 | P-03 | P-02 | P-01 | P-00 | P-17 | P-16 | P-15 | P-14 | P-13 | P-12 | P-11 | P-10 |
             * +------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+
             *
             *
             * The Configuration registers (registers 6 and 7) configure the direction of the I/O pins. If a bit in these
             * registers is set to 1, the corresponding port pin is enabled as an input with a high-impedance output driver.
             * If a bit in these registers is cleared to 0, the corresponding port pin is enabled as an output.
             * +-------------------------------------------------------+-------------------------------------------------------+
             * |             Register 0x06 = Configuration 0           |             Register 0x07 = Configuration 1           |
             * +------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+
             * | C-07 | C-06 | C-05 | C-04 | C-03 | C-02 | C-01 | C-00 | C-17 | C-16 | C-15 | C-14 | C-13 | C-12 | C-11 | C-10 |
             * +------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+------+
             *
             */

            var inputLevels = ReadInputLevels();
            var outputDrives = ReadOutputDrives();
            var polarities = ReadPolarities();
            var directions = ReadDirections();

            log.Debug("| InputLevels      | OutputDrives     | Polarities       | Directions       |");
            log.Debug($"| {ToBinaryString(inputLevels)} " +
                      $"| {ToBinaryString(outputDrives)} " +
                      $"| {ToBinaryString(polarities)} " +
                      $"| {ToBinaryString(directions)} |");
        }

        private ushort ReadInputLevels()
        {
            var registers = _i2CAdapter.Read(_deviceAddress, 2, new byte[] { 0x00 });
            var inputLevels = (ushort)((registers[1] << 8) | registers[0]);
            return inputLevels;
        }

        private ushort ReadOutputDrives()
        {
            var registers = _i2CAdapter.Read(_deviceAddress, 2, new byte[] { 0x02 });
            var outputDrives = (ushort)((registers[1] << 8) | registers[0]);
            return outputDrives;
        }

        private ushort ReadPolarities()
        {
            var registers = _i2CAdapter.Read(_deviceAddress, 2, new byte[] { 0x04 });
            var polarities = (ushort)((registers[1] << 8) | registers[0]);
            return polarities;
        }

        private ushort ReadDirections()
        {
            var registers = _i2CAdapter.Read(_deviceAddress, 2, new byte[] { 0x06 });
            var directions = (ushort)((registers[1] << 8) | registers[0]);
            return directions;
        }

        #endregion

        #region Bit Manipulations

        private static bool IsBitSet(ushort number, int bitIndex)
        {
            if (bitIndex < 0 || bitIndex > 15)
                throw new ArgumentOutOfRangeException(
                    nameof(bitIndex), "Must be a bit position index in the range of 0-15.");

            return (number & (1 << bitIndex)) != 0;
        }

        private static ushort SetBit(ushort number, int bitIndex)
        {
            if (bitIndex < 0 || bitIndex > 15)
                throw new ArgumentOutOfRangeException(
                    nameof(bitIndex), "Must be a bit position index in the range of 0-15.");

            return (ushort)(number | (1 << bitIndex));
        }

        private static ushort ClearBit(ushort number, int bitIndex)
        {
            if (bitIndex < 0 || bitIndex > 15)
                throw new ArgumentOutOfRangeException(
                    nameof(bitIndex), "Must be a bit position index in the range of 0-15.");

            var mask = (ushort)(1 << bitIndex);
            var invertedMask = (ushort)~mask;
            return (ushort)(number & invertedMask);
        }

        private static string ToBinaryString(ushort number)
        {
            return Convert.ToString(number, 2).PadLeft(16, '0');
        }

        #endregion

        #region GPIO Interface Implementation

        public void SetPinDirection(int pin, EDirection direction)
        {
            if (pin < 0 || pin > 15)
                throw new ArgumentOutOfRangeException(
                    nameof(pin), "Must be a bit position index in the range of 0-15.");

            var directions = ReadDirections();

            // Output = 0, Input = 1
            if (direction == EDirection.Output)
                directions = ClearBit(directions, pin);
            else
                directions = SetBit(directions, pin);

            var command = BitConverter.GetBytes(directions);
            _i2CAdapter.Write(_deviceAddress, new byte[] { 0x06 }, command);
            log.Debug($"Directions = {ToBinaryString(directions)}");
        }

        public void SetPinPull(int pin, EPull pull)
        {
            throw new NotSupportedException();
        }

        public void SetPinDrive(int pin, EDrive drive)
        {
            if (pin < 0 || pin > 15)
                throw new ArgumentOutOfRangeException(
                    nameof(pin), "Must be a bit position index in the range of 0-15.");

            var outputDrives = ReadOutputDrives();

            if (drive == EDrive.DriveLow)
                outputDrives = ClearBit(outputDrives, pin);
            else
                outputDrives = SetBit(outputDrives, pin);

            var command = BitConverter.GetBytes(outputDrives);
            _i2CAdapter.Write(_deviceAddress, new byte[] { 0x02 }, command);
            log.Debug($"OutputDrives = {ToBinaryString(outputDrives)}");
        }

        public ELevel GetPinLevel(int pin)
        {
            if (pin < 0 || pin > 15)
                throw new ArgumentOutOfRangeException(
                    nameof(pin), "Must be a bit position index in the range of 0-15.");

            var inputLevels = ReadInputLevels();
            log.Debug($"InputLevels = {ToBinaryString(inputLevels)}");

            var level = IsBitSet(inputLevels, pin) ? ELevel.High : ELevel.Low;
            return level;
        }

        public void SetPin(int pin, EDirection direction, EPull pull)
        {
            throw new NotImplementedException();
        }

        public void SetPin(int pin, EDirection direction, EPull pull, EDrive drive)
        {
            throw new NotImplementedException();
        }

        public (EDirection direction, EPull pull, ELevel level) GetPin(int pin)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}