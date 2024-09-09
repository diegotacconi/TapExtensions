/*
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
using OpenTap;
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

        [Display("Connect on Open", Order: 2)] public bool ConnectOnOpen { get; set; }

        [EnabledIf(nameof(ConnectOnOpen), true, HideIfDisabled = true)]
        [Display("Target Power", Order: 3, Description: "(Pin 4, 6)")]
        public ETargetPower TargetPower { get; set; }

        [EnabledIf(nameof(ConnectOnOpen), true, HideIfDisabled = true)]
        [Display("I2C Pull-up Resistors", Order: 4, Description: "(Pin 1, 3)")]
        public EI2cPullup I2CPullup { get; set; }

        [EnabledIf(nameof(ConnectOnOpen), true, HideIfDisabled = true)]
        [Display("I2C Bus bit rate", Order: 5)]
        [Unit("kHz")]
        public int I2CBitRateKhz { get; set; }

        [EnabledIf(nameof(ConnectOnOpen), true, HideIfDisabled = true)]
        [Display("SPI Bus bit rate", Order: 6)]
        [Unit("kHz")]
        public int SpiBitRateKhz { get; set; }

        #endregion

        private readonly object _instLock = new object();
        internal int AardvarkHandle;
        private bool _isInitialized;

        public Aardvark()
        {
            // Default values
            Name = nameof(Aardvark);
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
                    const EConfigMode configMode = EConfigMode.SPI_I2C;
                    Log.Debug($"Setting config mode to {configMode}");
                    var stat = AardvarkWrapper.aa_configure(AardvarkHandle, configMode);
                    if (stat != (int)configMode)
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
                ((II2C)this).SetBitRate((uint)I2CBitRateKhz);


                // ToDo: SPI Configuration


                // ToDo: net_aa_version(aardvark, ref version);

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