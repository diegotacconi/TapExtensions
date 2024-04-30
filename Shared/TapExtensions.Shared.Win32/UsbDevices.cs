using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace TapExtensions.Shared.Win32
{
    public static class UsbDevices
    {
        public class UsbSerialDevice
        {
            public string ComPort { get; set; }
            public string InstancePath { get; set; }
            public string Description { get; set; }
        }

        public static List<string> ListAllComPorts()
        {
            var messages = new List<string>();
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SerialPort"))
            {
                var mObjects = searcher.Get().Cast<ManagementBaseObject>().ToList();
                foreach (var mObject in mObjects)
                {
                    // https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-pnpentity?redirectedfrom=MSDN
                    var items = new List<string>
                    {
                        "Availability",
                        "Caption",
                        // "ClassGuid",
                        // "CompatibleID[]",
                        // "ConfigManagerErrorCode",
                        // "ConfigManagerUserConfig",
                        "CreationClassName",
                        "Description",
                        "DeviceID",
                        // "ErrorCleared",
                        // "ErrorDescription",
                        // "HardwareID[]",
                        // "InstallDate",
                        // "LastErrorCode",
                        // "Manufacturer",
                        "Name",
                        "PNPDeviceID",
                        // "PowerManagementCapabilities[]",
                        "PowerManagementSupported",
                        // "Present",
                        // "Service",
                        "Status",
                        "StatusInfo"
                        // "SystemCreationClassName",
                        // "SystemName",
                    };

                    foreach (var item in items)
                        messages.Add($"{item,-24} = {mObject[item]}");

                    messages.Add("---");
                }
            }

            return messages;
        }

        public static List<UsbSerialDevice> GetAllSerialDevices()
        {
            var devices = new List<UsbSerialDevice>();
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SerialPort"))
            {
                var mObjects = searcher.Get().Cast<ManagementBaseObject>().ToList();
                foreach (var mObject in mObjects)
                    devices.Add(new UsbSerialDevice
                    {
                        ComPort = mObject["DeviceID"].ToString(),
                        InstancePath = mObject["PNPDeviceID"].ToString(),
                        Description = mObject["Description"].ToString()
                    });
            }

            return devices;
        }

        public static UsbSerialDevice FindInstancePath(List<string> searchItems)
        {
            var found = new List<UsbSerialDevice>();
            var devices = GetAllSerialDevices();

            foreach (var searchItem in searchItems)
                foreach (var device in devices)
                    if (device.InstancePath.Contains(searchItem, StringComparison.OrdinalIgnoreCase))
                        found.Add(device);

            if (found.Count == 0)
                throw new Exception("Cannot find a serial port with " +
                                    $"USB Instance Path(s) of '{string.Join("', '", searchItems)}'");

            return found.First();
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