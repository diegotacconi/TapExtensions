﻿// Texas Instruments TCA6416A I/O Expander
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
using System.Linq;
using OpenTap;
using TapExtensions.Interfaces.Gpio;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Instruments.Gpio
{
    [Display("Texas Instruments TCA6416A",
        Groups: new[] { "TapExtensions", "Instruments", "Gpio" },
        Description: "16-bits of General Purpose Input/Output (I/O) expansion")]
    public class Tca6416A : Instrument, IGpio
    {
        #region Settings

        [Display("I2C Adapter", Order: 1)] public II2C I2CAdapter { get; set; }

        [Display("Device Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort DeviceAddress { get; set; } = 0x20;

        public Tca6416A()
        {
            Name = nameof(Tca6416A);
        }

        #endregion

        public byte[] ReadRegisters(out ushort level, out ushort drive, out ushort polarity, out ushort direction)
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
            // var register = _i2C.Read((ushort)_deviceAddress, 8, new byte[] { 0x00 });
            var register = I2CAdapter.Read(DeviceAddress, 8, new byte[] { 0x00 });
            level = (ushort)((register[1] << 8) | register[0]);
            drive = (ushort)((register[3] << 8) | register[2]);
            polarity = (ushort)((register[5] << 8) | register[4]);
            direction = (ushort)((register[7] << 8) | register[6]);
            return register;
        }

        #region GPIO Interface Implementation

        public void SetPinDirection(int pin, EDirection direction)
        {
            throw new NotImplementedException();
        }

        public void SetPinPull(int pin, EPull pull)
        {
            throw new NotImplementedException();
        }

        public void SetPinDrive(int pin, EDrive drive)
        {
            throw new NotImplementedException();
        }

        public void SetPin(int pin, EDirection direction, EPull pull)
        {
            throw new NotImplementedException();
        }

        public void SetPin(int pin, EDirection direction, EPull pull, EDrive drive)
        {
            throw new NotImplementedException();
        }

        public ELevel GetPinLevel(int pin)
        {
            // Debug start
            var registers = ReadRegisters(out var lvl, out var drive, out var polarity, out var dir);
            var binaryString = string.Join(" ", registers.Select(x => Convert.ToString(x, 2).PadLeft(8, '0')));
            Log.Debug($"Registers = {binaryString}");
            Log.Debug($"Lvl       = {Convert.ToString(lvl, 2).PadLeft(16, '0')}");
            Log.Debug($"Drive     = {Convert.ToString(drive, 2).PadLeft(16, '0')}");
            Log.Debug($"Polarity  = {Convert.ToString(polarity, 2).PadLeft(16, '0')}");
            Log.Debug($"Dir       = {Convert.ToString(dir, 2).PadLeft(16, '0')}");
            // Debug end


            var register = I2CAdapter.Read(DeviceAddress, 2, new byte[] { 0x00 });
            var level = (ushort)((register[1] << 8) | register[0]);

            Log.Debug($"Level = {Convert.ToString(level, 2).PadLeft(16, '0')}");

            // ToDo: Determine ELevel
            return ELevel.Low;
        }

        public (EDirection direction, EPull pull, ELevel level) GetPin(int pin)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /*
    // ReSharper disable InconsistentNaming
    public enum ETca6416Pin
    {
        P00_PIN_04 = 0b0000_0000_0000_0001,
        P01_PIN_05 = 0b0000_0000_0000_0010,
        P02_PIN_06 = 0b0000_0000_0000_0100,
        P03_PIN_07 = 0b0000_0000_0000_1000,
        P04_PIN_08 = 0b0000_0000_0001_0000,
        P05_PIN_09 = 0b0000_0000_0010_0000,
        P06_PIN_10 = 0b0000_0000_0100_0000,
        P07_PIN_11 = 0b0000_0000_1000_0000,
        P10_PIN_13 = 0b0000_0001_0000_0000,
        P11_PIN_14 = 0b0000_0010_0000_0000,
        P12_PIN_15 = 0b0000_0100_0000_0000,
        P13_PIN_16 = 0b0000_1000_0000_0000,
        P14_PIN_17 = 0b0001_0000_0000_0000,
        P15_PIN_18 = 0b0010_0000_0000_0000,
        P16_PIN_19 = 0b0100_0000_0000_0000,
        P17_PIN_20 = 0b1000_0000_0000_0000
    }
    */
}