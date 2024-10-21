using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c.Devices
{
    public class Mcp23017
    {
        private readonly II2C _i2C;
        private readonly int _deviceAddress;

        public Mcp23017(II2C i2C, int deviceAddress = 0x20)
        {
            _i2C = i2C;
            _deviceAddress = deviceAddress;
        }

        public byte[] ReadRegisters(out ushort direction, out ushort polarity, out ushort pull, out ushort level,
            out ushort drive)
        {
            /*
            +---------------+---------------------------------------------------------+-----------+
            |    Register   | bit7   bit6   bit5   bit4   bit3   bit2   bit1   bit0   |  Default  |
            +---------------+---------------------------------------------------------+-----------+
            | IODIRA   0x00 | IO7    IO6    IO5    IO4    IO3    IO2    IO1    IO0    | 1111 1111 |
            | IODIRB   0x01 | IO7    IO6    IO5    IO4    IO3    IO2    IO1    IO0    | 1111 1111 |
            | IPOLA    0x02 | IP7    IP6    IP5    IP4    IP3    IP2    IP1    IP0    | 0000 0000 |
            | IPOLB    0x03 | IP7    IP6    IP5    IP4    IP3    IP2    IP1    IP0    | 0000 0000 |
            | GPINTENA 0x04 | GPINT7 GPINT6 GPINT5 GPINT4 GPINT3 GPINT2 GPINT1 GPINT0 | 0000 0000 |
            | GPINTENB 0x05 | GPINT7 GPINT6 GPINT5 GPINT4 GPINT3 GPINT2 GPINT1 GPINT0 | 0000 0000 |
            | DEFVALA  0x06 | DEF7   DEF6   DEF5   DEF4   DEF3   DEF2   DEF1   DEF0   | 0000 0000 |
            | DEFVALB  0x07 | DEF7   DEF6   DEF5   DEF4   DEF3   DEF2   DEF1   DEF0   | 0000 0000 |
            | INTCONA  0x08 | IOC7   IOC6   IOC5   IOC4   IOC3   IOC2   IOC1   IOC0   | 0000 0000 |
            | INTCONB  0x09 | IOC7   IOC6   IOC5   IOC4   IOC3   IOC2   IOC1   IOC0   | 0000 0000 |
            | IOCON    0x0A | BANK   MIRROR SEQOP  DISSLW HAEN   ODR    INTPOL -      | 0000 0000 |
            | IOCON    0x0B | BANK   MIRROR SEQOP  DISSLW HAEN   ODR    INTPOL -      | 0000 0000 |
            | GPPUA    0x0C | PU7    PU6    PU5    PU4    PU3    PU2    PU1    PU0    | 0000 0000 |
            | GPPUB    0x0D | PU7    PU6    PU5    PU4    PU3    PU2    PU1    PU0    | 0000 0000 |
            | INTFA    0x0E | INT7   INT6   INT5   INT4   INT3   INT2   INT1   INTO   | 0000 0000 |
            | INTFB    0x0F | INT7   INT6   INT5   INT4   INT3   INT2   INT1   INTO   | 0000 0000 |
            | INTCAPA  0x10 | ICP7   ICP6   ICP5   ICP4   ICP3   ICP2   ICP1   ICP0   | 0000 0000 |
            | INTCAPB  0x11 | ICP7   ICP6   ICP5   ICP4   ICP3   ICP2   ICP1   ICP0   | 0000 0000 |
            | GPIOA    0x12 | GP7    GP6    GP5    GP4    GP3    GP2    GP1    GP0    | 0000 0000 |
            | GPIOB    0x13 | GP7    GP6    GP5    GP4    GP3    GP2    GP1    GP0    | 0000 0000 |
            | OLATA    0x14 | OL7    OL6    OL5    OL4    OL3    OL2    OL1    OL0    | 0000 0000 |
            | OLATB    0x15 | OL7    OL6    OL5    OL4    OL3    OL2    OL1    OL0    | 0000 0000 |
            +---------------+---------------------------------------------------------+-----------+

            IODIR: I/O DIRECTION
                1 = Pin is configured as an input.
                0 = Pin is configured as an output.

            IPOL: INPUT POLARITY
                1 = GPIO register bit reflects the opposite logic state of the input pin.
                0 = GPIO register bit reflects the same logic state of the input pin.

            GPPU: GPIO PULL-UP RESISTOR
                1 = Pull-up enabled
                0 = Pull-up disabled

            GPIO: GENERAL PURPOSE I/O (get input level)
                Reading from this register reads the port.
                Writing to this register modifies the Output Latch (OLAT) register.
                1 = Logic-high
                0 = Logic-low

            OLAT: OUTPUT LATCH (set output drive)
                Reading from this register results in a read of the OLAT and not the port itself.
                Writing to this register modifies the output latches that modifies the pins configured as outputs.
                1 = Logic-high
                0 = Logic-low

            Note: Pins GPA7 and GPB7 are output only for MCP23017.

            */

            var register = _i2C.Read((ushort)_deviceAddress, 22, new byte[] { 0x00 });
            direction = (ushort)((register[1] << 8) | register[0]);
            polarity = (ushort)((register[3] << 8) | register[2]);
            pull = (ushort)((register[13] << 8) | register[12]);
            level = (ushort)((register[19] << 8) | register[18]);
            drive = (ushort)((register[21] << 8) | register[20]);
            return register;
        }
    }
}