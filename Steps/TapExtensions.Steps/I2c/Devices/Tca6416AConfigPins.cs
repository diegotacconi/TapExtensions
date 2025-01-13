using System;
using System.Collections.Generic;
using System.Linq;
using OpenTap;
using TapExtensions.Interfaces.Gpio;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Steps.I2c.Devices
{
    [Display("Tca6416AConfigPins",
        Groups: new[] { "TapExtensions", "Steps", "I2c", "Devices" })]
    public class Tca6416AConfigPins : TestStep
    {
        [Display("Aardvark I2C Adapter", Order: 1)]
        public II2C I2CAdapter { get; set; }

        [Display("Device Address", Order: 2)]
        [Unit("Hex", StringFormat: "X2")]
        public ushort DeviceAddress { get; set; } = 0x20;

        public class Config : ValidatingObject
        {
            [Display("Pin Number", Order: 3)] public ETca6416Pin Pin { get; set; }

            [Display("Direction", Order: 4)] public EDirection Direction { get; set; }

            [EnabledIf(nameof(Direction), EDirection.Output)]
            [Display("Drive", Order: 5)]
            public EDrive Drive { get; set; }
        }

        [Display("List of Pins", Order: 6)]
        public List<Config> ListOfPins { get; set; } = new List<Config>
        {
            new Config { Pin = ETca6416Pin.P00_Pin04, Direction = EDirection.Input },
            new Config { Pin = ETca6416Pin.P01_Pin05, Direction = EDirection.Input },
            new Config { Pin = ETca6416Pin.P02_Pin06, Direction = EDirection.Input },
            new Config { Pin = ETca6416Pin.P03_Pin07, Direction = EDirection.Input }
        };

        public override void Run()
        {
            try
            {
                var tca6416A = new Tca6416A(I2CAdapter, DeviceAddress);

                // Debug start
                var registers = tca6416A.ReadRegisters(out var lvl, out var drive, out var polarity, out var dir);
                var binaryString = string.Join(" ", registers.Select(x => Convert.ToString(x, 2).PadLeft(8, '0')));
                Log.Debug($"Registers = {binaryString}");
                Log.Debug($"Lvl       = {Convert.ToString(lvl, 2).PadLeft(16, '0')}");
                Log.Debug($"Drive     = {Convert.ToString(drive, 2).PadLeft(16, '0')}");
                Log.Debug($"Polarity  = {Convert.ToString(polarity, 2).PadLeft(16, '0')}");
                Log.Debug($"Dir       = {Convert.ToString(dir, 2).PadLeft(16, '0')}");
                // Debug end

                /*
                foreach (var config in ListOfPins)
                {
                    tca6416A.SetPinDirection((int)config.Pin, config.Direction);

                    if (config.Direction == EDirection.Output)
                        tca6416A.SetPinDrive((int)config.Pin, config.Drive);
                }
                */

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