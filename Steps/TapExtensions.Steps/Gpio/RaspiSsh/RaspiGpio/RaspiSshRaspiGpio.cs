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

    $ sudo raspi-gpio get 20
    GPIO 20: level=0 fsel=0 func=INPUT

    $ sudo raspi-gpio set 5 ip pu
    $ sudo raspi-gpio get 5
    GPIO 5: level=1 func=INPUT pull=UP
*/

using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using OpenTap;
using TapExtensions.Interfaces.Ssh;

namespace TapExtensions.Steps.Gpio.RaspiSsh.RaspiGpio
{
    public abstract class RaspiSshRaspiGpio : TestStep
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

        #endregion

        #region Private Methods

        private protected void SendGpioInputCommand(string pin, EPinState state, EPull pull)
        {
            var retryCount = 3;
            for (var tryAttempt = 1; tryAttempt <= retryCount; tryAttempt++)
            {
                Raspi.SendSshQuery("sudo raspi-gpio set " + pin + " " + GetPinType(state) + " " +
                                   GetPull(pull), 5, out _);
                var substrings = GetGpioStatus(pin);

                if (substrings[2] == "INPUT")
                {
                    if (GetPull(pull) == "pd")
                    {
                        Log.Info("pin set PULL_DOWN!");
                    }
                    else if (GetPull(pull) == "pn")
                    {
                        Log.Info("pin set PULL_NONE!");
                    }
                    else if (GetPull(pull) == "pu")
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

        private protected void SendGpioOutputCommand(string pin, EPinState state)
        {
            var retryCount = 3;
            for (var tryAttempt = 1; tryAttempt <= retryCount; tryAttempt++)
            {
                Raspi.SendSshQuery("sudo raspi-gpio set " + pin + " op " + GetPinType(state), 5, out _);
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

                    break;
                }

                Log.Info("Not an output!");
                if (tryAttempt >= 3) throw new InvalidDataException(substrings[2]);
                Log.Info("Retrying verify...");
            }
        }

        private protected string[] GetGpioStatus(string pin)
        {
            Raspi.SendSshQuery("sudo raspi-gpio get " + pin, 5, out var verify);
            var regex = new Regex("level=(\\d+)\\s+.*func=([A-Z]+)");
            return regex.Split(verify);
        }

        private protected static string GetPinType(EPinState state)
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

        private protected static string GetPull(EPull pull)
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

        #endregion
    }
}