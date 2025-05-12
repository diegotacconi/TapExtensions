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
    raspi-gpio funcs will dump all the possible GPIO alt funcions in CSV format
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
      raspi-gpio set 20 op pn dh  Set GPIO20 to ouput with no pull and driving high

    $ raspi-gpio get 20
    GPIO 20: level=0 fsel=0 func=INPUT
*/

using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Gpio.RaspiSsh
{
    [Display("RaspiSshGpioSetPin",
        Groups: new[] { "TapExtensions", "Steps", "Gpio", "RaspiSsh" })]
    public class RaspiSshGpioSetPin : TestStep
    {
        public enum EPinState
        {
            High,
            Low,
            Input
        }

        public enum EPull
        {
            PullNone,
            PullDown,
            PullUp
        }

        #region Settings

        [Display("Raspi", Order: 1, Description: "RaspberryPi SSH Interface")]
        public ISshInstrument Raspi { get; set; }

        [Display("Pin Number", Order: 2)] public string Pin { get; set; }

        [Display("Pin State", Order: 3)] public EPinState PinState { get; set; }

        [Display("Pull", Order: 4)] public EPull Pull { get; set; }

        #endregion

        public override void Run()
        {
            try
            {
                Raspi.SendSshQuery("sudo raspi-gpio help", 5, out var check);
                if (check == "") throw new InvalidOperationException("You are missing raspi-gpio module!");

                switch (GetPinType(PinState))
                {
                    case "ip":
                        SendGpioInputCommand();
                        break;

                    case "dl":
                        SendGpioOutputCommand();
                        break;

                    case "dh":
                        SendGpioOutputCommand();
                        break;
                }

                UpgradeVerdict(Verdict.Pass);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                UpgradeVerdict(Verdict.Fail);
            }
        }

        private void SendGpioInputCommand()
        {
            var retryCount = 3;
            for (var tryAttempt = 1; tryAttempt <= retryCount; tryAttempt++)
            {
                Raspi.SendSshQuery("sudo raspi-gpio set " + Pin + " " + GetPinType(PinState) + " " +
                                   GetPull(Pull), 5, out _);
                var substrings = GetGpioStatus();

                if (substrings[2] == "INPUT")
                {
                    if (GetPull(Pull) == "pd")
                    {
                        Log.Info("pin set PULL_DOWN!");
                    }
                    else if (GetPull(Pull) == "pn")
                    {
                        Log.Info("pin set PULL_NONE!");
                    }
                    else if (GetPull(Pull) == "pu")
                    {
                        Log.Info("pin set PULL_UP!");
                    }
                    else
                    {
                        Log.Info("verification failed!");
                        throw new InvalidDataException(substrings[1]);
                    }

                    break;
                }

                Log.Info("Not an input!");
                if (tryAttempt >= 3) throw new InvalidDataException(substrings[2]);
                Log.Info("Retrying verify...");
            }
        }

        private void SendGpioOutputCommand()
        {
            var retryCount = 3;
            for (var tryAttempt = 1; tryAttempt <= retryCount; tryAttempt++)
            {
                Raspi.SendSshQuery("sudo raspi-gpio set " + Pin + " op " + GetPinType(PinState), 5, out _);
                var substrings = GetGpioStatus();

                if (substrings[2] == "OUTPUT")
                {
                    if (substrings[1] == "0" && GetPinType(PinState) == "dl")
                    {
                        Log.Info("pin set LOW!");
                    }
                    else if (substrings[1] == "1" && GetPinType(PinState) == "dh")
                    {
                        Log.Info("pin set HIGH!");
                    }
                    else
                    {
                        Log.Info("verification failed!");
                        throw new InvalidDataException(substrings[1]);
                    }

                    break;
                }

                Log.Info("Not an output!");
                if (tryAttempt >= 3) throw new InvalidDataException(substrings[2]);
                Log.Info("Retrying verify...");
            }
        }

        private string[] GetGpioStatus()
        {
            Raspi.SendSshQuery("sudo raspi-gpio get " + Pin, 5, out var verify);
            var regex = new Regex("level=(\\d+)\\s+.*func=([A-Z]+)");
            return regex.Split(verify);
        }

        private static string GetPinType(EPinState state)
        {
            string returnState;

            switch (state)
            {
                case EPinState.High:
                    returnState = "dh";
                    break;

                case EPinState.Low:
                    returnState = "dl";
                    break;

                case EPinState.Input:
                    returnState = "ip";
                    break;

                default:
                    throw new InvalidEnumArgumentException(nameof(state));
            }

            return returnState;
        }

        private static string GetPull(EPull pull)
        {
            string returnPull;

            switch (pull)
            {
                case EPull.PullUp:
                    returnPull = "pu";
                    break;

                case EPull.PullDown:
                    returnPull = "pd";
                    break;

                case EPull.PullNone:
                    returnPull = "pn";
                    break;

                default:
                    throw new InvalidEnumArgumentException(nameof(pull));
            }

            return returnPull;
        }
    }
}