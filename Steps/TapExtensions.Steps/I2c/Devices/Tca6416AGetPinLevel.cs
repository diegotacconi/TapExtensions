using System;
using OpenTap;
using TapExtensions.Interfaces.Gpio;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c.Devices
{
    [Display("Tca6416AGetPinLevel",
        Groups: new[] { "TapExtensions", "Steps", "I2c", "Devices" })]
    public class Tca6416AGetPinLevel : TestStep
    {
        [Display("Aardvark I2C Adapter", Order: 1)]
        public II2C I2CAdapter { get; set; }

        [Display("Device Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort DeviceAddress { get; set; } = 0x20;

        [Display("Pin Number", Order: 3)] public ETca6416Pin PinNumber { get; set; }

        [Display("Expected Pin Level", Order: 4)]
        public ELevel ExpectedLevel { get; set; }

        public override void Run()
        {
            try
            {
                var tca6416A = new Tca6416A(I2CAdapter, DeviceAddress);
                var measuredLevel = tca6416A.GetPinLevel((int)PinNumber);
                if (measuredLevel != ExpectedLevel)
                    throw new InvalidOperationException(
                        $"Pin {PinNumber} measured an input level of {measuredLevel}, " +
                        $"which is not equal to the expected level of {ExpectedLevel}.");

                Log.Debug($"{PinNumber} measured {measuredLevel}");
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