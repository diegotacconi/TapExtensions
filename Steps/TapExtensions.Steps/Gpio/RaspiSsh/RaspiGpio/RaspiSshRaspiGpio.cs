/*
   raspi-gpio set writes directly to the GPIO control registers
   ignoring whatever else may be using them (such as Linux drivers) -
   it is designed as a debug tool, only use it if you know what you
   are doing and at your own risk!

   The raspi-gpio tool is designed to help hack / debug BCM283x GPIO.
   Running raspi-gpio with the help argument prints this help.
   raspi-gpio can get and print the state of a GPIO (or all GPIOs)
   and can be used to set the function, pulls and value of a GPIO.
   raspi-gpio must be run as root.
   Use:
     raspi-gpio get [GPIO]
   OR
     raspi-gpio set <GPIO> [options]
   OR
     raspi-gpio funcs [GPIO]
   OR
     raspi-gpio raw

   GPIO is a comma-separated list of pin numbers or ranges (without spaces),
   e.g. 4 or 18-21 or 7,9-11

   Note that omitting [GPIO] from raspi-gpio get prints all GPIOs.
   raspi-gpio funcs will dump all the possible GPIO alt functions in CSV format
   or if [GPIO] is specified the alternate funcs just for that specific GPIO.

   Valid [options] for raspi-gpio set are:
     ip      set GPIO as input
     op      set GPIO as output
     a0-a5   set GPIO to alternate function alt0-alt5
     pu      set GPIO in-pad pull up
     pd      set GPIO pin-pad pull down
     pn      set GPIO pull none (no pull)
     dh      set GPIO to drive to high (1) level (only valid if set to be an output)
     dl      set GPIO to drive low (0) level (only valid if set to be an output)

   Examples:
     raspi-gpio get              Prints state of all GPIOs one per line
     raspi-gpio get 20           Prints state of GPIO20
     raspi-gpio get 20,21        Prints state of GPIO20 and GPIO21
     raspi-gpio set 20 a5        Set GPIO20 to ALT5 function (GPCLK0)
     raspi-gpio set 20 pu        Enable GPIO20 ~50k in-pad pull up
     raspi-gpio set 20 pd        Enable GPIO20 ~50k in-pad pull down
     raspi-gpio set 20 op        Set GPIO20 to be an output
     raspi-gpio set 20 dl        Set GPIO20 to output low/zero (must already be set as an output)
     raspi-gpio set 20 ip pd     Set GPIO20 to input with pull down
     raspi-gpio set 35 a0 pu     Set GPIO35 to ALT0 function (SPI_CE1_N) with pull up
     raspi-gpio set 20 op pn dh  Set GPIO20 to output with no pull and driving high
*/

using System;
using System.Collections.Generic;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Gpio.RaspiSsh.RaspiGpio
{
    public abstract class RaspiSshRaspiGpio : TestStep
    {
        #region Enums

        public enum EDirection
        {
            Input,
            Output
        }

        public enum EPull
        {
            PullNone,
            PullDown,
            PullUp
        }

        public enum EDrive
        {
            DriveLow,
            DriveHigh
        }

        public enum ELevel
        {
            Low,
            High
        }

        #endregion

        #region Settings

        [Display("Raspi Instrument", Order: 1, Description: "RaspberryPi SSH Instrument Interface")]
        public ISshInstrument Raspi { get; set; }

        #endregion

        private protected bool IsValidPinNumber(int pinNumber)
        {
            var validPins = new List<int>
            {
                2, // GPIO=02, Header=03
                3, // GPIO=03, Header=05
                4, // GPIO=04, Header=07
                5, // GPIO=05, Header=29
                6, // GPIO=06, Header=31
                7, // GPIO=07, Header=26
                8, // GPIO=08, Header=24
                9, // GPIO=09, Header=21
                10, // GPIO=10, Header=19
                11, // GPIO=11, Header=23
                12, // GPIO=12, Header=32
                13, // GPIO=13, Header=33
                14, // GPIO=14, Header=08
                15, // GPIO=15, Header=10
                16, // GPIO=16, Header=36
                17, // GPIO=17, Header=11
                18, // GPIO=18, Header=12
                19, // GPIO=19, Header=35
                20, // GPIO=20, Header=38
                21, // GPIO=21, Header=40
                22, // GPIO=22, Header=15
                23, // GPIO=23, Header=16
                24, // GPIO=24, Header=18
                25, // GPIO=25, Header=22
                26, // GPIO=26, Header=37
                27 // GPIO=27, Header=13
            };
            return validPins.Contains(pinNumber);
        }

        private protected void SetPin(int pin, EDirection direction, EPull pull)
        {
            var cmd = $"sudo raspi-gpio set {pin} {EnumToString(direction)} {EnumToString(pull)}";
            if (!Raspi.SendSshQuery(cmd, 5, out _))
                throw new InvalidOperationException(
                    $"Exit status was not 0, when executing to the command of '{cmd}'");

            var (directionResponse, pullResponse, _) = GetPin(pin);

            if (direction != directionResponse)
                throw new InvalidOperationException(
                    $"Error setting direction to {direction}");

            if (pull != pullResponse)
                throw new InvalidOperationException(
                    $"Error setting pull to {pull}");
        }

        private protected void SetPin(int pin, EDirection direction, EPull pull, EDrive drive)
        {
            var cmd = $"sudo raspi-gpio set {pin} {EnumToString(direction)} {EnumToString(pull)} {EnumToString(drive)}";
            if (!Raspi.SendSshQuery(cmd, 5, out _))
                throw new InvalidOperationException(
                    $"Exit status was not 0, when executing to the command of '{cmd}'");

            var (directionResponse, pullResponse, levelResponse) = GetPin(pin);

            if (direction != directionResponse)
                throw new InvalidOperationException(
                    $"Error setting direction to {direction}");

            if (pull != pullResponse)
                throw new InvalidOperationException(
                    $"Error setting pull to {pull}");

            if (DriveToLevel(drive) != levelResponse)
                throw new InvalidOperationException(
                    $"Error setting drive to {drive} (the level measured was {levelResponse})");
        }

        private protected (EDirection direction, EPull pull, ELevel level) GetPin(int pin)
        {
            var cmd = $"sudo raspi-gpio get {pin}";
            if (!Raspi.SendSshQuery(cmd, 5, out var response))
                throw new InvalidOperationException(
                    $"Exit status was not 0, when executing to the command of '{cmd}'");

            var (direction, pull, level) = ParseResponse(response.Trim());
            return (direction, pull, level);
        }

        private protected static ELevel DriveToLevel(EDrive drive)
        {
            ELevel level;

            switch (drive)
            {
                case EDrive.DriveLow:
                    level = ELevel.Low;
                    break;
                case EDrive.DriveHigh:
                    level = ELevel.High;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(drive), drive, null);
            }

            return level;
        }

        private protected static string EnumToString(Enum key)
        {
            var optionsDictionary = new Dictionary<Enum, string>
            {
                { EDirection.Input, "ip" },
                { EDirection.Output, "op" },
                { EPull.PullNone, "pn" },
                { EPull.PullDown, "pd" },
                { EPull.PullUp, "pu" },
                { EDrive.DriveLow, "dl" },
                { EDrive.DriveHigh, "dh" },
                { ELevel.Low, "lo" },
                { ELevel.High, "hi" }
            };

            if (!optionsDictionary.TryGetValue(key, out var value))
                throw new InvalidOperationException(
                    $"Cannot find key of '{key}' in {nameof(optionsDictionary)}");

            return value;
        }

        private protected static Enum StringToEnum(string key)
        {
            var responsesDictionary = new Dictionary<string, Enum>
            {
                { "INPUT", EDirection.Input }, // func=INPUT
                { "OUTPUT", EDirection.Output }, // func=OUTPUT
                { "NONE", EPull.PullNone }, // pull=NONE
                { "DOWN", EPull.PullDown }, // pull=DOWN
                { "UP", EPull.PullUp }, // pull=UP
                { "0", ELevel.Low }, // level=0
                { "1", ELevel.High } // level=1
            };

            if (!responsesDictionary.TryGetValue(key, out var value))
                throw new InvalidOperationException(
                    $"Cannot find key of '{key}' in {nameof(responsesDictionary)}");

            return value;
        }

        private protected static string GetStringBetween(string text, string before, string after)
        {
            var x = 0; // If the 'before' string is empty, then use the whole 'text' string
            if (before != string.Empty)
            {
                x = text.IndexOf(before, StringComparison.Ordinal);
                if (x < 0)
                    throw new InvalidOperationException(
                        $"Cannot find the string '{before}' in '{text}'");
                x += before.Length;
            }

            if (after == string.Empty)
                return text.Substring(x);

            var y = text.IndexOf(after, x, StringComparison.Ordinal);
            if (y < 0)
                throw new InvalidOperationException(
                    $"Cannot find the string '{after}' in '{text}'");

            return text.Substring(x, y - x);
        }

        private protected static (EDirection direction, EPull pull, ELevel level) ParseResponse(string response)
        {
            // GPIO 2: level=1 alt=0 func=SDA1 pull=UP
            // GPIO 5: level=0 func=INPUT pull=DOWN
            // GPIO 5: level=0 func=OUTPUT pull=NONE

            if (!response.Contains("level=") || !response.Contains("func=") || !response.Contains("pull="))
                throw new InvalidOperationException(
                    $"Cannot parse the response string of '{response}'");

            var direction = (EDirection)StringToEnum(GetStringBetween(response, "func=", " "));
            var pull = (EPull)StringToEnum(GetStringBetween(response, "pull=", ""));
            var level = (ELevel)StringToEnum(GetStringBetween(response, "level=", " "));

            return (direction, pull, level);
        }
    }
}