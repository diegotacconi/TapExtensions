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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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

        [Display("Connection Address", Order: 1,
            Description: "Port Number, or Serial Number, or List of serial numbers separated by semicolons.\n\n" +
                         "Examples:\n 1\n 2\n 2237174516\n 2237174516; 2239556166; 2239578705")]
        public string ConnectionAddress { get; set; }

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
            ConnectionAddress = "0";
            ConnectOnOpen = true;
            TargetPower = ETargetPower.Off;
            I2CPullup = EI2cPullup.Off;
            I2CBitRateKhz = 100;
            SpiBitRateKhz = 1000;

            // Validation rules
            Rules.Add(ValidateConnectionAddress,
                "Not valid", nameof(ConnectionAddress));
            Rules.Add(() => I2CBitRateKhz >= 1 && I2CBitRateKhz <= 800,
                "I2C bit rate must be between 1 and 800 kHz", nameof(I2CBitRateKhz));
            Rules.Add(() => SpiBitRateKhz >= 125 && SpiBitRateKhz <= 8000,
                "SPI bit rate must be between 125 kHz and 8 MHz", nameof(SpiBitRateKhz));
        }

        private enum EAddressType
        {
            PortNumber,
            SerialNumber,
            ListOfSerialNumbers
        }

        private bool ValidateConnectionAddress()
        {
            try
            {
                GetAddressType();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private EAddressType GetAddressType()
        {
            if (Regex.IsMatch(ConnectionAddress, @"^[0-9]{1}$"))
                return EAddressType.PortNumber;

            if (Regex.IsMatch(ConnectionAddress, @"^[0-9]{10}$"))
                return EAddressType.SerialNumber;

            if (Regex.IsMatch(ConnectionAddress, @"^[0-9]{10}\s*(;\s*[0-9]{10}\s*)*$"))
                return EAddressType.ListOfSerialNumbers;

            throw new InvalidOperationException(
                $"{nameof(ConnectionAddress)} is not valid");
        }


        private class Device
        {
            public ushort PortNumber { get; set; }
            public uint SerialNumber { get; set; }
        }

        private List<Device> FindDevices()
        {
            const int quantity = 10;
            var portNumbers = new ushort[quantity];
            var serialNumbers = new uint[quantity];

            var numDevicesFound =
                AardvarkWrapper.net_aa_find_devices_ext(quantity, portNumbers, quantity, serialNumbers);

            if (numDevicesFound < 1)
                throw new InvalidOperationException("Aardvark devices not found");

            var devices = new List<Device>();
            for (var i = 0; i < numDevicesFound; i++)
                devices.Add(new Device { PortNumber = portNumbers[i], SerialNumber = serialNumbers[i] });

            Log.Debug($"Found {numDevicesFound} Aardvark device(s), with serial number(s) of " +
                      $"'{string.Join("', '", devices.Select(x => x.SerialNumber).ToList())}'.");

            return devices;
        }

        private Device FindDevice()
        {
            var devices = FindDevices();

            switch (GetAddressType())
            {
                case EAddressType.PortNumber:
                {
                    if (!ushort.TryParse(ConnectionAddress, out var portNumber))
                        throw new InvalidOperationException(
                            $"Cannot parse PortNumber from {nameof(ConnectionAddress)} of '{ConnectionAddress}'");

                    var device = devices.FirstOrDefault(x => x.PortNumber == portNumber);
                    if (device == null)
                        throw new InvalidOperationException(
                            $"Cannot find PortNumber of '{portNumber}' in list of devices");

                    return device;
                }
                case EAddressType.SerialNumber:
                {
                    if (!uint.TryParse(ConnectionAddress, out var serialNumber))
                        throw new InvalidOperationException(
                            $"Cannot parse SerialNumber from {nameof(ConnectionAddress)} of '{ConnectionAddress}'");

                    var device = devices.FirstOrDefault(x => x.SerialNumber == serialNumber);
                    if (device == null)
                        throw new InvalidOperationException(
                            $"Cannot find SerialNumber of '{serialNumber}' in list of devices");

                    return device;
                }
                case EAddressType.ListOfSerialNumbers:
                {
                    var requests = ConnectionAddress.Split(';')
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();

                    var device = new Device();
                    foreach (var request in requests)
                    {
                        if (!uint.TryParse(request, out var serialNumber))
                            throw new InvalidOperationException(
                                $"Cannot parse SerialNumber from '{request}'");

                        device = devices.FirstOrDefault(x => x.SerialNumber == serialNumber);
                        if (device != null)
                            break;
                    }

                    if (device == null)
                        throw new InvalidOperationException(
                            "Cannot find SerialNumber in list of devices");

                    return device;
                }
                default:
                    throw new InvalidOperationException(
                        "Cannot find matching device");
            }
        }

        public override void Open()
        {
            base.Open();

            if (ConnectOnOpen)
                Initialize();
        }

        public override void Close()
        {
            CheckIfInitialized();

            // Restore powerMask power to off
            if (TargetPower != ETargetPower.Off)
                SetTargetPower(ETargetPower.Off);

            AardvarkWrapper.net_aa_close(AardvarkHandle);
            AardvarkHandle = 0;
            _isInitialized = false;
            base.Close();
        }

        private void Initialize()
        {
            lock (_instLock)
            {
                if (_isInitialized)
                    throw new InvalidOperationException($"{Name} already initialized");

                var device = FindDevice();
                Log.Debug($"Opening Aardvark device with port number of '{device.PortNumber}' " +
                          $"and serial number of '{device.SerialNumber}'");

                AardvarkHandle = AardvarkWrapper.net_aa_open(device.PortNumber);

                if (AardvarkHandle < 0)
                {
                    var errorMsg = Marshal.PtrToStringAnsi(AardvarkWrapper.net_aa_status_string(AardvarkHandle));
                    throw new InvalidOperationException($"{Name}: Error {AardvarkHandle}, {errorMsg}");
                }

                // Configure
                _isInitialized = true;

                try
                {
                    const EConfigMode configMode = EConfigMode.SPI_I2C;
                    Log.Debug($"Setting config mode to {configMode}");
                    var stat = AardvarkWrapper.net_aa_configure(AardvarkHandle, configMode);
                    if (stat != (int)configMode)
                    {
                        var errorMsg = Marshal.PtrToStringAnsi(AardvarkWrapper.net_aa_status_string(AardvarkHandle));
                        throw new InvalidOperationException($"{Name}: Error {AardvarkHandle}, {errorMsg}");
                    }

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
            }
        }

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

                var status = AardvarkWrapper.net_aa_target_power(AardvarkHandle, (byte)powerMask);
                if (status == (int)powerMask)
                    return;

                var errorMsg = Marshal.PtrToStringAnsi(AardvarkWrapper.net_aa_status_string(status));
                throw new InvalidOperationException($"{Name}: Error {status}, {errorMsg}");
            }
        }

        private void LogDebugData(string infoStart, byte[] data, int stat = 0)
        {
            if (string.IsNullOrEmpty(infoStart))
                throw new InvalidOperationException("LogDebugData Value(infoStart) cannot be null or empty.");

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
    }
}