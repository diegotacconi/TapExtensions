using System;
using System.Collections.Generic;
using System.Linq;
using OpenTap;
using TapExtensions.Interfaces.Gpio;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Gpio
{
    public abstract class RaspiGpio : TestStep, IRaspiGpio
    {
        #region Settings

        [Display("Raspi", Order: 1)] public ISecureShell Raspi { get; set; }

        // ReSharper disable InconsistentNaming
        public enum ERaspiGpio
        {
            GPIO_02_PINHDR_03 = 2,
            GPIO_03_PINHDR_05 = 3,
            GPIO_04_PINHDR_07 = 4,
            GPIO_05_PINHDR_29 = 5,
            GPIO_06_PINHDR_31 = 6,
            GPIO_07_PINHDR_26 = 7,
            GPIO_08_PINHDR_24 = 8,
            GPIO_09_PINHDR_21 = 9,
            GPIO_10_PINHDR_19 = 10,
            GPIO_11_PINHDR_23 = 11,
            GPIO_12_PINHDR_32 = 12,
            GPIO_13_PINHDR_33 = 13,
            GPIO_14_PINHDR_08 = 14,
            GPIO_15_PINHDR_10 = 15,
            GPIO_16_PINHDR_36 = 16,
            GPIO_17_PINHDR_11 = 17,
            GPIO_18_PINHDR_12 = 18,
            GPIO_19_PINHDR_35 = 19,
            GPIO_20_PINHDR_38 = 20,
            GPIO_21_PINHDR_40 = 21,
            GPIO_22_PINHDR_15 = 22,
            GPIO_23_PINHDR_16 = 23,
            GPIO_24_PINHDR_18 = 24,
            GPIO_25_PINHDR_22 = 25,
            GPIO_26_PINHDR_37 = 26,
            GPIO_27_PINHDR_13 = 27
        }

        #endregion

        #region GPIO Interface Implementation

        public void SetPinDirection(int pin, EDirection direction)
        {
            var cmd = $"sudo pinctrl set {pin} {EnumToString(direction)}";
            if (!Raspi.SendSshQuery(cmd, 5, out _))
                throw new InvalidOperationException(
                    $"Exit status was not 0, when executing to the command of '{cmd}'");

            // ToDo: verify by sending "sudo pinctrl get" and checking response
        }

        public void SetPinPull(int pin, EPull pull)
        {
            var cmd = $"sudo pinctrl set {pin} {EnumToString(pull)}";
            if (!Raspi.SendSshQuery(cmd, 5, out _))
                throw new InvalidOperationException(
                    $"Exit status was not 0, when executing to the command of '{cmd}'");

            // ToDo: verify by sending "sudo pinctrl get" and checking response
        }

        public void SetPinDrive(int pin, EDrive drive)
        {
            var cmd = $"sudo pinctrl set {pin} {EnumToString(drive)}";
            if (!Raspi.SendSshQuery(cmd, 5, out _))
                throw new InvalidOperationException(
                    $"Exit status was not 0, when executing to the command of '{cmd}'");

            // ToDo: verify by sending "sudo pinctrl get" and checking response
        }

        public ELevel GetPinLevel(int pin)
        {
            var cmd = $"sudo pinctrl get {pin}";
            if (!Raspi.SendSshQuery(cmd, 5, out var response))
                throw new InvalidOperationException(
                    $"Exit status was not 0, when executing to the command of '{cmd}'");

            var measuredLevel = (ELevel)ParseLevel(response);
            return measuredLevel;
        }

        #endregion

        #region Private Methods

        private readonly Dictionary<Enum, string> dictionary =
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

        private protected string EnumToString(Enum key)
        {
            if (!dictionary.TryGetValue(key, out var value))
                throw new InvalidOperationException(
                    $"Cannot find key of '{key}' in GPIO dictionary");

            return value;
        }

        private protected Enum StringToEnum(string value)
        {
            if (!dictionary.ContainsValue(value))
                throw new InvalidOperationException(
                    $"Cannot find value of '{value}' in GPIO dictionary");

            var key = dictionary.FirstOrDefault(x => x.Value == value).Key;
            return key;
        }

        private protected Enum ParseLevel(string response)
        {
            // "%2d: %2s %s %s | %s // %s%s%s\n"
            // "%2d: %2s    %s | %s // %s%s%s\n"
            //    6: ip    pu | hi // GPIO6 = input
            var levelString = GetStringBetween(response, " | ", " // ");
            var levelEnum = StringToEnum(levelString);
            return levelEnum;
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

        #endregion
    }
}