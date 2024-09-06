﻿/*
 *    Connector Pinout:
 *    https://www.totalphase.com/support/articles/200468316-aardvark-i2c-spi-host-adapter-user-manual/#s2.1
 *
 *                     ┌──────────┐
 *    ┌────────────────┘          └────────────────┐
 *    │ 1 SCL    3 SDA    5 MISO   7 SCLK    9 SS  │
 *    │                                            │
 *    │ 2 GND    4 PWR    6 PWR    8 MOSI   10 GND │
 *    └────────────────────────────────────────────┘
 */

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using OpenTap;
using TapExtensions.Interfaces.Gpio;
using TapExtensions.Interfaces.I2c;

namespace TapExtensions.Instruments.MultipleInterfaces.Aardvark
{
    [Display("TotalPhase Aardvark",
        Groups: new[] { "TapExtensions", "Instruments", "MultipleInterfaces" },
        Description: "TotalPhase Aardvark I2C/SPI Host Adapter")]
    public partial class Aardvark : Instrument
    {
        #region Settings

        [Display("Device Number", Order: 1)] public int DevNumber { get; set; }

        [Display("Config Mode", Order: 2)] public EConfigMode ConfigMode { get; set; }

        [Display("Connect on Open", Order: 3)] public bool ConnectOnOpen { get; set; }

        [EnabledIf(nameof(ConnectOnOpen), true, HideIfDisabled = true)]
        [Display("Target Power", Order: 4, Description: "(Pin 4, 6)")]
        public ETargetPower TargetPower { get; set; }

        [EnabledIf(nameof(ConnectOnOpen), true, HideIfDisabled = true)]
        [Display("I2C Pull-up Resistors", Order: 5, Description: "(Pin 1, 3)")]
        public EI2cPullup I2CPullup { get; set; }

        [EnabledIf(nameof(ConnectOnOpen), true, HideIfDisabled = true)]
        [EnabledIf(nameof(ConfigMode), EConfigMode.GPIO_I2C, EConfigMode.SPI_I2C, HideIfDisabled = true)]
        [Display("I2C Bus bit rate", Order: 6)]
        [Unit("kHz")]
        public int I2CBitRateKhz { get; set; }

        [EnabledIf(nameof(ConnectOnOpen), true, HideIfDisabled = true)]
        [EnabledIf(nameof(ConfigMode), EConfigMode.SPI_GPIO, EConfigMode.SPI_I2C, HideIfDisabled = true)]
        [Display("SPI Bus bit rate", Order: 7)]
        [Unit("kHz")]
        public int SpiBitRateKhz { get; set; }

        #endregion

        #region Debug GPIO Control

        [XmlIgnore]
        [Browsable(true)]
        [EnabledIf(nameof(ConfigMode), EConfigMode.GPIO_ONLY, EConfigMode.GPIO_I2C,
            EConfigMode.SPI_GPIO, HideIfDisabled = true)]
        [Display("Debug GPIO Control", Order: 20, Group: "Debug")]
        public bool DebugGpioControl { get; set; } = false;

        [XmlIgnore]
        [Browsable(true)]
        [EnabledIf(nameof(ConfigMode), EConfigMode.GPIO_ONLY, EConfigMode.GPIO_I2C,
            EConfigMode.SPI_GPIO, HideIfDisabled = true)]
        [EnabledIf(nameof(DebugGpioControl), HideIfDisabled = true)]
        [Display("Pin", Order: 21, Group: "Debug")]
        public EAardvarkPin Pin { get; set; } = EAardvarkPin.GPIO_05_PINHDR_09_SS;

        [XmlIgnore]
        [Browsable(true)]
        [EnabledIf(nameof(ConfigMode), EConfigMode.GPIO_ONLY, EConfigMode.GPIO_I2C,
            EConfigMode.SPI_GPIO, HideIfDisabled = true)]
        [EnabledIf(nameof(DebugGpioControl), HideIfDisabled = true)]
        [Display("Pin Mode", Order: 22, Group: "Debug")]
        public EPinMode PinMode { get; set; } = EPinMode.Input;

        [XmlIgnore]
        [Browsable(true)]
        [EnabledIf(nameof(ConfigMode), EConfigMode.GPIO_ONLY, EConfigMode.GPIO_I2C,
            EConfigMode.SPI_GPIO, HideIfDisabled = true)]
        [EnabledIf(nameof(DebugGpioControl), HideIfDisabled = true)]
        [EnabledIf(nameof(PinMode), EPinMode.Output, HideIfDisabled = true)]
        [Display("Pin State", Order: 22, Group: "Debug")]
        public EPinState PinState { get; set; } = EPinState.Low;

        [Browsable(true)]
        [EnabledIf(nameof(ConfigMode), EConfigMode.GPIO_ONLY, EConfigMode.GPIO_I2C,
            EConfigMode.SPI_GPIO, HideIfDisabled = true)]
        [EnabledIf(nameof(DebugGpioControl), HideIfDisabled = true)]
        [EnabledIf(nameof(PinMode), EPinMode.Output, HideIfDisabled = true)]
        [Display("Set", Order: 23, Group: "Debug")]
        public void SetPinStateButton()
        {
            SetPinMode((int)Pin, PinMode);
            SetPinState((int)Pin, PinState);
        }

        [Browsable(true)]
        [EnabledIf(nameof(ConfigMode), EConfigMode.GPIO_ONLY, EConfigMode.GPIO_I2C,
            EConfigMode.SPI_GPIO, HideIfDisabled = true)]
        [EnabledIf(nameof(DebugGpioControl), HideIfDisabled = true)]
        [EnabledIf(nameof(PinMode), EPinMode.Input, EPinMode.InputPullDown, EPinMode.InputPullUp,
            HideIfDisabled = true)]
        [Display("Get", Order: 24, Group: "Debug")]
        public void GetPinStateButton()
        {
            SetPinMode((int)Pin, PinMode);
            GetPinState((int)Pin);
        }

        #endregion

        private readonly object _instLock = new object();
        internal int AardvarkHandle;
        private bool _isInitialized;

        public Aardvark()
        {
            // Default values
            Name = nameof(Aardvark);
            ConfigMode = EConfigMode.SPI_I2C;
            ConnectOnOpen = true;
            TargetPower = ETargetPower.Off;
            I2CPullup = EI2cPullup.Off;
            I2CBitRateKhz = 100;
            SpiBitRateKhz = 1000;

            // Validation rules
            Rules.Add(() => DevNumber >= 0,
                "Must be greater or equal to zero", nameof(DevNumber));
            Rules.Add(() => I2CBitRateKhz >= 1 && I2CBitRateKhz <= 800,
                "I2C bit rate must be between 1 and 800 kHz", nameof(I2CBitRateKhz));
            Rules.Add(() => SpiBitRateKhz >= 125 && SpiBitRateKhz <= 8000,
                "SPI bit rate must be between 125 kHz and 8 MHz", nameof(SpiBitRateKhz));
        }

        public override void Open()
        {
            base.Open();

            // ToDo: net_aa_configure(aardvark, config);
            // ToDo: net_aa_version(aardvark, ref version);
            // ToDo: return Marshal.PtrToStringAnsi(net_aa_status_string(status));
            // ToDo: return net_aa_unique_id(aardvark);
            // ToDo: return net_aa_port(aardvark);

            lock (_instLock)
            {
                if (_isInitialized)
                    throw new ApplicationException("Aardvark(s) already initialized!");

                if (!ConnectOnOpen)
                    return;

                // Find Devices
                var maxNumDevices = 16;
                var devices = new ushort[maxNumDevices];
                var numDevicesFound = AardvarkWrapper.aa_find_devices(maxNumDevices, devices);

                if (numDevicesFound < 1)
                    throw new ApplicationException("Number of Aardvark devices found should be more than 0.");

                Log.Debug("Found " + numDevicesFound + " Aardvarks. Initializing device number " + DevNumber + ".");

                // Open an Aardvark device
                const int stepDelay = 333;
                var tryDelay = 0;
                var aExt = new AardvarkWrapper.AardvarkExt();
                do
                {
                    TapThread.Sleep(tryDelay);
                    tryDelay += stepDelay; // 0, 333, 666, 999...
                    AardvarkHandle = AardvarkWrapper.aa_open_ext(DevNumber, ref aExt);
                } while (AardvarkHandle < 0 && tryDelay < 1000);

                if (AardvarkHandle < 0)
                {
                    var errorMsg = AardvarkWrapper.aa_status_string(AardvarkHandle);
                    throw new ApplicationException($"{Name}: Error {AardvarkHandle}, {errorMsg}");
                }

                // Get Unique ID
                var uniqueId = AardvarkWrapper.aa_unique_id(AardvarkHandle);
                if (uniqueId < 1)
                    throw new ApplicationException("Aardvark's unique ID should be non-zero if valid.");

                Log.Debug("Aardvark<" + DevNumber + "> has serial number of " + uniqueId + ".");


                // Configure
                _isInitialized = true;

                try
                {
                    Log.Debug($"Setting config mode to {ConfigMode}");
                    var stat = AardvarkWrapper.aa_configure(AardvarkHandle, ConfigMode);
                    if (stat != (int)ConfigMode)
                        throw new ApplicationException("ERRor[" + stat +
                                                       "] when configuring aardvark to SPI+I2C mode.");

                    // Settings below must add, if some application stops working. SpiInit set these, but if application do not run it???
                    //stat = AardvarkWrapper.net_aa_spi_configure(AardvarkHandle, AardvarkSpiPolarity.AaSpiPolRisingFalling,
                    //                       AardvarkSpiPhase.AaSpiPhaseSampleSetup, AardvarkSpiBitorder.AaSpiBitorderMsb);
                    //if (stat != (int)AardvarkStatus.AA_OK)
                    //    throw new ApplicationException("SPI initial spi_configure return: " + stat);
                }
                catch (Exception)
                {
                    _isInitialized = false;
                    throw;
                }

                SetTargetPower(TargetPower);
                SetPullupResistors(I2CPullup);


                // I2C Configuration
                if (ConfigMode == EConfigMode.GPIO_I2C || ConfigMode == EConfigMode.SPI_I2C)
                    ((II2C)this).SetBitRate((uint)I2CBitRateKhz);


                // SPI Configuration
                if (ConfigMode == EConfigMode.SPI_GPIO || ConfigMode == EConfigMode.SPI_I2C)
                {
                    // throw new NotImplementedException();
                }


                // GPIO Configuration
                if (ConfigMode == EConfigMode.GPIO_ONLY || ConfigMode == EConfigMode.GPIO_I2C ||
                    ConfigMode == EConfigMode.SPI_GPIO)
                    if (ConfigMode == EConfigMode.GPIO_ONLY)
                    {
                        // Make sure the charge has dissipated on those lines
                        AardvarkWrapper.aa_gpio_set(AardvarkHandle, 0x00);
                        AardvarkWrapper.aa_gpio_direction(AardvarkHandle, 0xff);

                        // By default all GPIO pins are inputs.  Writing 1 to the
                        // bit position corresponding to the appropriate line will
                        // configure that line as an output
                        AardvarkWrapper.aa_gpio_direction(AardvarkHandle,
                            (byte)(AardvarkGpioBits.AA_GPIO_SS | AardvarkGpioBits.AA_GPIO_SCL));

                        // By default all GPIO outputs are logic low.  Writing a 1
                        // to the appropriate bit position will force that line
                        // high provided it is configured as an output.  If it is
                        // not configured as an output the line state will be
                        // cached such that if the direction later changed, the
                        // latest output value for the line will be enforced.
                        AardvarkWrapper.aa_gpio_set(AardvarkHandle, (byte)AardvarkGpioBits.AA_GPIO_SCL);
                        Log.Debug("Setting SCL to logic low");

                        // The get method will return the line states of all inputs.
                        // If a line is not configured as an input the value of
                        // that particular bit position in the mask will be 0.
                        var val = (byte)AardvarkWrapper.aa_gpio_get(AardvarkHandle);

                        // Check the state of SCK
                        if ((val & (byte)AardvarkGpioBits.AA_GPIO_SCK) != 0)
                            Log.Debug("Read the SCK line as logic high");
                        else
                            Log.Debug("Read the SCK line as logic low");

                        // Optionally we can set passive pullups on certain lines.
                        // This can prevent input lines from floating.  The pullup
                        // configuration is only valid for lines configured as inputs.
                        // If the line is not currently input the requested pullup
                        // state will take effect only if the line is later changed
                        // to be an input line.
                        AardvarkWrapper.aa_gpio_pullup(AardvarkHandle,
                            (byte)(AardvarkGpioBits.AA_GPIO_MISO | AardvarkGpioBits.AA_GPIO_MOSI));

                        // Now reading the MISO line should give a logic high,
                        // provided there is nothing attached to the Aardvark
                        // adapter that is driving the pin low.
                        val = (byte)AardvarkWrapper.aa_gpio_get(AardvarkHandle);
                        if ((val & (byte)AardvarkGpioBits.AA_GPIO_MISO) != 0)
                            Log.Debug(
                                "Read the MISO line as logic high (passive pullup)");
                        else
                            Log.Debug(
                                "Read the MISO line as logic low (is pin driven low?)");


                        // Now do a 1000 gets from the GPIO to test performance
                        for (var i = 0; i < 1000; ++i)
                            AardvarkWrapper.aa_gpio_get(AardvarkHandle);

                        int oldval, newval;

                        // Demonstrate use of aa_gpio_change
                        AardvarkWrapper.aa_gpio_direction(AardvarkHandle, 0x00);
                        oldval = AardvarkWrapper.aa_gpio_get(AardvarkHandle);

                        Log.Debug("Calling aa_gpio_change for 2 seconds...");
                        newval = AardvarkWrapper.aa_gpio_change(AardvarkHandle, 2000);

                        if (newval != oldval)
                            Log.Debug("  GPIO inputs changed.\n");
                        else
                            Log.Debug("  GPIO inputs did not change.\n");
                    }


                // TODO: net_aa_version(aardvark, ref version);

                // Status
                //var status = AardvarkStatus.AaUnableToOpen;
                //var statusString = Marshal.PtrToStringAnsi(AardvarkWrapper.net_aa_status_string((int)status));
                //Log.Debug("Aardvark<" + DevNumber + "> status is :" + statusString + ".");

                Log.Debug("Aardvark<" + DevNumber + "> initialized with try " + tryDelay / stepDelay + ".");
            }
        }

        public override void Close()
        {
            CheckIfInitialized();

            // Restore powerMask power to off
            if (TargetPower != ETargetPower.Off)
                SetTargetPower(ETargetPower.Off);

            AardvarkWrapper.aa_close(AardvarkHandle);
            AardvarkHandle = 0;
            _isInitialized = false;
            base.Close();
        }

        #region Private Methods

        private void CheckIfInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException($"{Name} not initialized");
        }

        private void SetTargetPower(ETargetPower powerMask)
        {
            lock (_instLock)
            {
                CheckIfInitialized();
                Log.Debug($"Setting target power to {powerMask}");

                var status = AardvarkWrapper.aa_target_power(AardvarkHandle, (byte)powerMask);
                if (status == (int)powerMask)
                    return;

                var errorMsg = AardvarkWrapper.aa_status_string(status);
                throw new InvalidOperationException($"{Name}: Error {status}, {errorMsg}");
            }
        }

        private void LogDebugData(string infoStart, byte[] data, int stat = 0)
        {
            if (string.IsNullOrEmpty(infoStart))
                throw new ArgumentException("LogDebugData Value(infoStart) cannot be null or empty.",
                    nameof(infoStart));

            if (data == null)
            {
                Log.Debug(infoStart + " have NOT data(=null). Status: " + stat);
                return;
            }

            CheckIfInitialized();
            lock (_instLock)
            {
                var dataLen = data.Length;
                const int maxLen = 100;
                var hexValues = "0x( ";

                for (var i = 0; i < dataLen; i++)
                {
                    if (i > 0)
                        hexValues += " ";

                    hexValues += data[i].ToString("X2");

                    if (hexValues.Length > maxLen)
                    {
                        // Shorten long logs
                        hexValues += " ---";
                        break;
                    }
                }

                // All Aardvark API functions return an integer which is either the result of the
                // transaction, or a status code if negative.
                if (stat < 0)
                    Log.Debug(infoStart + hexValues + " ). Status: " + stat);
                else
                    Log.Debug(infoStart + hexValues + " )");
            }
        }

        #endregion
    }
}