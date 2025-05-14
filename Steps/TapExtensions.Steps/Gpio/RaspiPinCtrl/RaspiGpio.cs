using OpenTap;
using System;
using System.Collections.Generic;
using System.Linq;
using TapExtensions.Interfaces.Gpio;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Gpio.RaspiPinCtrl
{
    public abstract class RaspiGpio : TestStep
    {
        [Display("Raspi", Order: 1, Description: "RaspberryPi SSH Interface")]
        public ISshInstrument Raspi { get; set; }

        public void SetPin(int pin, EDirection direction, EPull pull)
        {
            var cmd = $"sudo pinctrl -e set {pin} {EnumToString(direction)} {EnumToString(pull)}";
            if (!Raspi.SendSshQuery(cmd, 5, out var response))
                throw new InvalidOperationException(
                    $"Exit status was not 0, when executing to the command of '{cmd}'");

            VerifyResponse(response, direction);
            VerifyResponse(response, pull);
        }

        public void SetPin(int pin, EDirection direction, EPull pull, EDrive drive)
        {
            var cmd = $"sudo pinctrl -e set {pin} {EnumToString(direction)} {EnumToString(pull)} {EnumToString(drive)}";
            if (!Raspi.SendSshQuery(cmd, 5, out var response))
                throw new InvalidOperationException(
                    $"Exit status was not 0, when executing to the command of '{cmd}'");

            VerifyResponse(response, direction);
            VerifyResponse(response, pull);
            VerifyResponse(response, drive);
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

        private static readonly Dictionary<Enum, string> Dictionary =
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
            if (!Dictionary.TryGetValue(key, out var value))
                throw new InvalidOperationException(
                    $"Cannot find key of '{key}' in GPIO dictionary");

            return value;
        }

        private protected static Enum StringToEnum(string value)
        {
            if (!Dictionary.ContainsValue(value))
                throw new InvalidOperationException(
                    $"Cannot find value of '{value}' in GPIO dictionary");

            var key = Dictionary.FirstOrDefault(x => x.Value == value).Key;
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

        private protected static void VerifyResponse(string response, EDirection direction)
        {
            var (directionResponse, _, _) = ParseResponse(response);
            if (direction != directionResponse)
                throw new InvalidOperationException(
                    $"Error setting direction to {direction}");
        }

        private protected static void VerifyResponse(string response, EPull pull)
        {
            var (_, pullResponse, _) = ParseResponse(response);
            if (pull != pullResponse)
                throw new InvalidOperationException(
                    $"Error setting pull to {pull}");
        }

        private protected static void VerifyResponse(string response, EDrive drive)
        {
            var (_, _, levelResponse) = ParseResponse(response);
            if (DriveToLevel(drive) != levelResponse)
                throw new InvalidOperationException(
                    $"Error setting drive to {drive} (the level measured was {levelResponse})");
        }
    }
}
