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