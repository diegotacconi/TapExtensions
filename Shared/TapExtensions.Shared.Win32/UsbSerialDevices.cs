﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace TapExtensions.Shared.Win32
{
    public static class UsbSerialDevices
    {
        private static readonly object Obj = new object();

        public class UsbSerialDevice
        {
            public string ComPort { get; set; }
            public string UsbAddress { get; set; }
            public string Description { get; set; }
        }

        public static List<UsbSerialDevice> GetAllSerialDevices()
        {
            var devices = new List<UsbSerialDevice>();

            lock (Obj)
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SerialPort"))
                {
                    var mObjects = searcher.Get().Cast<ManagementBaseObject>().ToList();
                    foreach (var mObject in mObjects)
                        devices.Add(new UsbSerialDevice
                        {
                            ComPort = mObject["DeviceID"].ToString(),
                            UsbAddress = mObject["PNPDeviceID"].ToString(),
                            Description = mObject["Description"].ToString()
                        });
                }
            }

            return devices;
        }

        public static UsbSerialDevice FindUsbAddress(List<string> searchItems)
        {
            if (searchItems.Count == 0)
                throw new InvalidOperationException("List of items cannot be empty");

            var finds = new List<UsbSerialDevice>();
            var devices = GetAllSerialDevices();

            foreach (var searchItem in searchItems)
            {
                var find = FindUsbAddress(devices, searchItem);
                if (find != null)
                {
                    finds.Add(find);
                    break;
                }
            }

            if (finds.Count == 0)
                throw new InvalidOperationException(
                    "Cannot find a serial port with USB Address(es) of " +
                    $"'{string.Join("', '", searchItems)}'");

            return finds.First();
        }

        private static UsbSerialDevice FindUsbAddress(List<UsbSerialDevice> devices, string searchItem)
        {
            var found = new List<UsbSerialDevice>();

            foreach (var device in devices)
                if (device.UsbAddress.Contains(searchItem, StringComparison.OrdinalIgnoreCase))
                    found.Add(device);

            if (found.Count > 1)
                throw new InvalidOperationException(
                    "Found multiple devices of " +
                    $"'{string.Join("', '", found.Select(x => x.UsbAddress))}'; " +
                    $"when searching for the USB Address of '{searchItem}'. " +
                    "Please enter a search pattern that will return a single serial port device");

            return found.Any() ? found.First() : null;
        }
    }

    internal static class StringExtensions
    {
        public static bool Contains(this string source, string substring, StringComparison comp)
        {
            if (substring == null)
                throw new ArgumentNullException(nameof(substring), "substring cannot be null");

            if (!Enum.IsDefined(typeof(StringComparison), comp))
                throw new ArgumentException($"'{comp}' is not a member of StringComparison", nameof(comp));

            return source.IndexOf(substring, comp) >= 0;
        }
    }
}