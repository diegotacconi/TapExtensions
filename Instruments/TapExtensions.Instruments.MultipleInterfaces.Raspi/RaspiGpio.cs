using System;
using System.IO;
using System.Text.RegularExpressions;
using OpenTap;
using TapExtensions.Interfaces.Gpio;

namespace TapExtensions.Instruments.MultipleInterfaces.Raspi
{
    public partial class Raspi : IGpio
    {
        #region GPIO Interface Implementation

        public void SetPinMode(int pin, EPinInputMode mode)
        {
            throw new NotImplementedException();
        }

        public void SetPinState(int pin, EPinState state)
        {
            // ToDo:
            //    /sys/class/gpio/gpio11/direction
            //    /sys/class/gpio/gpio11/value
            //    /dev/gpiochipN
            //    sudo usermod -a -G gpio <username>

            CheckIfConnected();

            switch (GetPinType(state))
            {
                case "ip":
                    // SendGpioInputCommand(pin, state, mode);
                    break;
                case "dl":
                    SendGpioOutputCommand(pin, state);
                    break;
                case "dh":
                    SendGpioOutputCommand(pin, state);
                    break;
            }
        }

        public EPinState GetPinState(int pin)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private Methods

        private void SendGpioInputCommand(int pin, EPinState state, EPinInputMode mode)
        {
            var command = "sudo raspi-gpio set " + pin + " " + GetPinType(state) + " " + GetInputMode(mode);
            if (!SendSshQuery(command, 5, out _))
                throw new InvalidOperationException(
                    $"Exit status was not 0, when executing to the command of '{command}'");

            var substrings = GetGpioStatus(pin);

            if (substrings[2] == "INPUT")
            {
                if (GetInputMode(mode) == "pd")
                {
                    Log.Info("pin set PULL_DOWN!");
                }
                else if (GetInputMode(mode) == "pn")
                {
                    Log.Info("pin set PULL_NONE!");
                }
                else if (GetInputMode(mode) == "pu")
                {
                    Log.Info("pin set PULL_UP!");
                }
                else
                {
                    Log.Info("verification failed!");
                    throw new InvalidDataException(substrings[1]);
                }
            }
            else
            {
                Log.Info("Not an input!");
                throw new InvalidDataException(substrings[2]);
            }
        }

        private void SendGpioOutputCommand(int pin, EPinState state)
        {
            var command = $"sudo raspi-gpio set {pin} op {GetPinType(state)}";
            if (!SendSshQuery(command, 5, out _))
                throw new InvalidOperationException(
                    $"Exit status was not 0, when executing to the command of '{command}'");

            var substrings = GetGpioStatus(pin);

            if (substrings[2] == "OUTPUT")
            {
                if (substrings[1] == "0" && GetPinType(state) == "dl")
                {
                    Log.Info("pin set LOW!");
                }
                else if (substrings[1] == "1" && GetPinType(state) == "dh")
                {
                    Log.Info("pin set HIGH!");
                }
                else
                {
                    Log.Info("verification failed!");
                    throw new InvalidDataException(substrings[1]);
                }
            }
            else
            {
                Log.Info("Not an output!");
                throw new InvalidDataException(substrings[2]);
            }
        }

        private string[] GetGpioStatus(int pin)
        {
            /*
                pi@lmi:~ $ sudo raspi-gpio get
                BANK0 (GPIO 0 to 27):
                GPIO 0: level=1 func=INPUT pull=UP
                GPIO 1: level=1 func=INPUT pull=UP
                GPIO 2: level=1 alt=0 func=SDA1 pull=UP
                GPIO 3: level=1 alt=0 func=SCL1 pull=UP
                GPIO 4: level=1 func=INPUT pull=NONE
                GPIO 5: level=1 func=INPUT pull=UP
                GPIO 6: level=1 func=INPUT pull=UP
                GPIO 7: level=1 func=OUTPUT pull=UP
                GPIO 8: level=1 func=OUTPUT pull=UP
                GPIO 9: level=0 alt=0 func=SPI0_MISO pull=DOWN
                GPIO 10: level=0 alt=0 func=SPI0_MOSI pull=DOWN
                GPIO 11: level=0 alt=0 func=SPI0_SCLK pull=DOWN
                GPIO 12: level=0 func=INPUT pull=DOWN
                GPIO 13: level=0 func=INPUT pull=DOWN
                GPIO 14: level=1 alt=5 func=TXD1 pull=NONE
                GPIO 15: level=1 alt=5 func=RXD1 pull=UP
                GPIO 16: level=0 func=INPUT pull=DOWN
                GPIO 17: level=0 func=INPUT pull=DOWN
                GPIO 18: level=0 func=INPUT pull=DOWN
                GPIO 19: level=0 func=INPUT pull=DOWN
                GPIO 20: level=0 func=INPUT pull=DOWN
                GPIO 21: level=0 func=INPUT pull=DOWN
                GPIO 22: level=0 func=INPUT pull=DOWN
                GPIO 23: level=0 func=INPUT pull=DOWN
                GPIO 24: level=0 func=INPUT pull=DOWN
                GPIO 25: level=0 func=INPUT pull=DOWN
                GPIO 26: level=0 func=INPUT pull=DOWN
                GPIO 27: level=0 func=INPUT pull=DOWN
                BANK1 (GPIO 28 to 45):
                GPIO 28: level=1 alt=5 func=RGMII_MDIO pull=UP
                GPIO 29: level=0 alt=5 func=RGMII_MDC pull=DOWN
                GPIO 30: level=0 alt=3 func=CTS0 pull=UP
                GPIO 31: level=0 alt=3 func=RTS0 pull=NONE
                GPIO 32: level=1 alt=3 func=TXD0 pull=NONE
                GPIO 33: level=1 alt=3 func=RXD0 pull=UP
                GPIO 34: level=1 alt=3 func=SD1_CLK pull=NONE
                GPIO 35: level=1 alt=3 func=SD1_CMD pull=UP
                GPIO 36: level=1 alt=3 func=SD1_DAT0 pull=UP
                GPIO 37: level=1 alt=3 func=SD1_DAT1 pull=UP
                GPIO 38: level=1 alt=3 func=SD1_DAT2 pull=UP
                GPIO 39: level=1 alt=3 func=SD1_DAT3 pull=UP
                GPIO 40: level=0 alt=0 func=PWM1_0 pull=NONE
                GPIO 41: level=0 alt=0 func=PWM1_1 pull=NONE
                GPIO 42: level=0 func=OUTPUT pull=UP
                GPIO 43: level=1 func=INPUT pull=UP
                GPIO 44: level=1 func=INPUT pull=UP
                GPIO 45: level=1 func=INPUT pull=UP
                BANK2 (GPIO 46 to 53):
                GPIO 46: level=0 func=INPUT pull=UP
                GPIO 47: level=0 func=INPUT pull=UP
                GPIO 48: level=0 func=INPUT pull=DOWN
                GPIO 49: level=0 func=INPUT pull=DOWN
                GPIO 50: level=0 func=INPUT pull=DOWN
                GPIO 51: level=0 func=INPUT pull=DOWN
                GPIO 52: level=0 func=INPUT pull=DOWN
                GPIO 53: level=0 func=INPUT pull=DOWN
                pi@lmi:~ $
             */

            var command = $"sudo raspi-gpio get {pin}";
            if (!SendSshQuery(command, 5, out var response))
                throw new InvalidOperationException(
                    $"Exit status was not 0, when executing to the command of '{command}'");

            var regex = new Regex("level=(\\d+)\\s+.*func=([A-Z]+)");
            return regex.Split(response);
        }

        private static string GetPinType(EPinState state)
        {
            string pinType;

            switch (state)
            {
                case EPinState.OutputHigh:
                    pinType = "dh";
                    break;

                case EPinState.OutputLow:
                    pinType = "dl";
                    break;

                case EPinState.Input:
                    pinType = "ip";
                    break;

                default:
                    throw new ArgumentException(
                        $"Case not found for {nameof(state)}={state}");
            }

            return pinType;
        }

        private static string GetInputMode(EPinInputMode mode)
        {
            string inputMode;

            switch (mode)
            {
                case EPinInputMode.PullUp:
                    inputMode = "pu";
                    break;

                case EPinInputMode.PullDown:
                    inputMode = "pd";
                    break;

                case EPinInputMode.PullNone:
                    inputMode = "pn";
                    break;

                default:
                    throw new ArgumentException(
                        $"Case not found for {nameof(mode)}={mode}");
            }

            return inputMode;
        }

        #endregion
    }

    // ReSharper disable InconsistentNaming
    public enum ERaspiPin
    {
        GPIO_02_PINHDR_03 = 3,
        GPIO_03_PINHDR_05 = 5,
        GPIO_04_PINHDR_07 = 7,
        GPIO_14_PINHDR_08 = 8,
        GPIO_15_PINHDR_10 = 10,
        GPIO_17_PINHDR_11 = 11,
        GPIO_18_PINHDR_12 = 12,
        GPIO_27_PINHDR_13 = 13,
        GPIO_22_PINHDR_15 = 15,
        GPIO_23_PINHDR_16 = 16,
        GPIO_24_PINHDR_18 = 18,
        GPIO_10_PINHDR_19 = 19,
        GPIO_09_PINHDR_21 = 21,
        GPIO_25_PINHDR_22 = 22,
        GPIO_11_PINHDR_23 = 23,
        GPIO_08_PINHDR_24 = 24,
        GPIO_07_PINHDR_26 = 26,
        GPIO_05_PINHDR_29 = 29,
        GPIO_06_PINHDR_31 = 31,
        GPIO_12_PINHDR_32 = 32,
        GPIO_13_PINHDR_33 = 33,
        GPIO_19_PINHDR_35 = 35,
        GPIO_16_PINHDR_36 = 36,
        GPIO_26_PINHDR_37 = 37,
        GPIO_20_PINHDR_38 = 38,
        GPIO_21_PINHDR_40 = 40
    }
}