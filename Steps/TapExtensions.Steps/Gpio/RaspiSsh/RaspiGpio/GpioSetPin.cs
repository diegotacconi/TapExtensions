using System;
using OpenTap;

namespace TapExtensions.Steps.Gpio.RaspiSsh.RaspiGpio
{
    [Display("GpioSetPin",
        Groups: new[] { "TapExtensions", "Steps", "Gpio", "RaspiSsh", "RaspiGpio" })]
    public class GpioSetPin : RaspiSshRaspiGpio
    {
        [Display("Pin Number", Order: 2)] public string Pin { get; set; }

        [Display("Pin State", Order: 3)] public EPinState PinState { get; set; }

        [Display("Pin Pull", Order: 4)] public EPull Pull { get; set; }

        public override void Run()
        {
            try
            {
                Raspi.SendSshQuery("sudo raspi-gpio help", 5, out var check);
                if (check == "") throw new InvalidOperationException("You are missing raspi-gpio module!");

                switch (GetPinType(PinState))
                {
                    case "ip":
                        SendGpioInputCommand(Pin, PinState, Pull);
                        break;

                    case "dl":
                        SendGpioOutputCommand(Pin, PinState);
                        break;

                    case "dh":
                        SendGpioOutputCommand(Pin, PinState);
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
    }
}