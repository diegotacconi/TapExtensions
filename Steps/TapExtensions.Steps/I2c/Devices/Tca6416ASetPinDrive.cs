using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c.Devices
{
    [Display("Tca6416ASetPinDrive",
        Groups: new[] { "TapExtensions", "Steps", "I2c", "Devices" })]
    public class Tca6416ASetPinDrive : TestStep
    {
        [Display("I2C Adapter", Order: 1)] public II2C I2CAdapter { get; set; }

        [Display("Device Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort DeviceAddress { get; set; } = 0x20;

        [Display("Pin Number", Order: 3)] public ETca6416Pin PinNumber { get; set; }

        [Display("Pin Output Drive", Order: 4)]
        public EDrive Drive { get; set; }

        public override void Run()
        {
            try
            {
                var tca6416A = new Tca6416A(I2CAdapter, DeviceAddress);
                tca6416A.SetPinDrive((int)PinNumber, Drive);

                Log.Debug($"Set {PinNumber} to {Drive}");
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