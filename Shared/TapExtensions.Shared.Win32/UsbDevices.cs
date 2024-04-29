using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace TapExtensions.Shared.Win32
{
    public static class UsbDevices
    {
        public static void ShowAllComPorts()
        {
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
                        Console.WriteLine($"{item,-24} = {mObject[item]}");

                    Console.WriteLine("---");
                }
            }
        }
    }
}