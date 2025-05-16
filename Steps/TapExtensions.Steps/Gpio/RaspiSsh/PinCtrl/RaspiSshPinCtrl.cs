// https://github.com/raspberrypi/utils/blob/master/pinctrl

/*
   pinctrl can get and print the state of a GPIO (or all GPIOs)
   and can be used to set the function, pulls and value of a GPIO.
   pinctrl must be run as root.
   Use:
     pinctrl [-p] [-v] get [GPIO]
   OR
     pinctrl [-p] [-v] [-e] set <GPIO> [options]
   OR
     pinctrl [-p] [-v] poll <GPIO>
   OR
     pinctrl [-p] [-v] funcs [GPIO]
   OR
     pinctrl [-p] [-v] lev [GPIO]
   OR
     pinctrl -c <chip> [funcs] [GPIO]

   GPIO is a comma-separated list of GPIO names, numbers or ranges (without
   spaces), e.g. 4 or 18-21 or BT_ON,9-11

   Note that omitting [GPIO] from "pinctrl get" prints all GPIOs.
   If the -p option is given, GPIO numbers are replaced by pin numbers on the
   40-way header. If the -v option is given, the output is more verbose. Including
   the -e option in a "set" causes pinctrl to echo back the new pin states.
   pinctrl funcs will dump all the possible GPIO alt functions in CSV format
   or if [GPIO] is specified the alternate funcs just for that specific GPIO.
   The -c option allows the alt functions (and only the alt function) for a named
   chip to be displayed, even if that chip is not present in the current system.

   Valid [options] for pinctrl set are:
     ip      set GPIO as input
     op      set GPIO as output
     a0-a8   set GPIO to alt function in the range 0 to 8 (range varies by model)
     no      set GPIO to no function (NONE)
     pu      set GPIO in-pad pull up
     pd      set GPIO in-pad pull down
     pn      set GPIO pull none (no pull)
     dh      set GPIO to drive high (1) level (only valid if set to be an output)
     dl      set GPIO to drive low (0) level (only valid if set to be an output)

   Examples:
     pinctrl get              Prints state of all GPIOs one per line
     pinctrl get 10           Prints state of GPIO10
     pinctrl get 10,11        Prints state of GPIO10 and GPIO11
     pinctrl set 10 a2        Set GPIO10 to fsel 2 function (nand_wen_clk)
     pinctrl -e set 10 pu     Enable GPIO10 ~50k in-pad pull up, echoing the result
     pinctrl set 10 pd        Enable GPIO10 ~50k in-pad pull down
     pinctrl set 10 op        Set GPIO10 to be an output
     pinctrl set 10 dl        Set GPIO10 to output low/zero (must already be set as an output)
     pinctrl set 10 ip pd     Set GPIO10 to input with pull down
     pinctrl set 35 a1 pu     Set GPIO35 to fsel 1 (jtag_2_clk) with pull up
     pinctrl set 20 op pn dh  Set GPIO20 to output with no pull and driving high
     pinctrl lev 4            Prints the level (1 or 0) of GPIO4
     pinctrl -c bcm2835 9-11  Display the alt functions for GPIOs 9-11 on bcm2835
 */

using System;
using System.Collections.Generic;
using System.Linq;
using OpenTap;
using TapExtensions.Interfaces.Gpio;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Gpio.RaspiSsh.PinCtrl
{
    public abstract class RaspiSshPinCtrl : TestStep //, IGpioDevice
    {
        #region Settings

        [Display("Raspi Instrument", Order: 1, Description: "RaspberryPi SSH Instrument Interface")]
        public ISshInstrument Raspi { get; set; }

        #endregion

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

        public ELevel GetPinLevel(int pin)
        {
            throw new NotImplementedException();
        }

        public void SetPin(int pin, EDirection? direction = null, EPull? pull = null, EDrive? drive = null)
        {
            // Build command
            var cmd = $"sudo pinctrl -e set {pin}" +
                      $" {(direction.HasValue ? EnumToString(direction.Value) : string.Empty)}" +
                      $" {(pull.HasValue ? EnumToString(pull.Value) : string.Empty)}" +
                      $" {(drive.HasValue ? EnumToString(drive.Value) : string.Empty)}";

            // Send command
            if (!Raspi.SendSshQuery(cmd, 5, out var response))
                throw new InvalidOperationException(
                    $"Exit status was not 0, when executing to the command of '{cmd}'");

            // Verify response
            var (directionResponse, pullResponse, levelResponse) = ParseResponse(response);

            if (direction.HasValue)
                if (direction.Value != directionResponse)
                    throw new InvalidOperationException(
                        $"Error setting direction to {direction.Value}");

            if (pull.HasValue)
                if (pull.Value != pullResponse)
                    throw new InvalidOperationException(
                        $"Error setting pull to {pull.Value}");

            if (drive.HasValue)
                if (DriveToLevel(drive.Value) != levelResponse)
                    throw new InvalidOperationException(
                        $"Error setting drive to {drive.Value} (the level measured was {levelResponse})");
        }

        public (EDirection direction, EPull pull, ELevel level) GetPin(int pin)
        {
            var cmd = $"sudo pinctrl get {pin}";
            if (!Raspi.SendSshQuery(cmd, 5, out var response))
                throw new InvalidOperationException(
                    $"Exit status was not 0, when executing to the command of '{cmd}'");

            var (direction, pull, level) = ParseResponse(response);
            return (direction, pull, level);
        }

        #endregion

        #region Private Methods

        private static readonly Dictionary<Enum, string> OptionsDictionary =
            new Dictionary<Enum, string>
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
            if (!OptionsDictionary.TryGetValue(key, out var value))
                throw new InvalidOperationException(
                    $"Cannot find key of '{key}' in GPIO dictionary");

            return value;
        }

        private protected static Enum StringToEnum(string value)
        {
            if (!OptionsDictionary.ContainsValue(value))
                throw new InvalidOperationException(
                    $"Cannot find value of '{value}' in GPIO dictionary");

            var key = OptionsDictionary.FirstOrDefault(x => x.Value == value).Key;
            return key;
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
            // "%2d: %2s %s %s | %s // %s%s%s\n"
            // "%2d: %2s    %s | %s // %s%s%s\n"
            //    6: ip    pu | hi // GPIO6 = input

            // var pin = int.Parse(GetStringBetween(response, "", ": "));
            var middleSection = GetStringBetween(response, ": ", " | ");
            var fields = middleSection.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var direction = (EDirection)StringToEnum(fields.First());
            var pull = (EPull)StringToEnum(fields.Last());
            var level = (ELevel)StringToEnum(GetStringBetween(response, " | ", " // "));

            return (direction, pull, level);
        }

        #endregion
    }
}