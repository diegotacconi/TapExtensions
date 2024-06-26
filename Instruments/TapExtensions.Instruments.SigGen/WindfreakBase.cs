﻿using System;
using System.Collections.Generic;
using System.IO.Ports;
using OpenTap;
using TapExtensions.Shared.Win32;

namespace TapExtensions.Instruments.SigGen
{
    public abstract class WindfreakBase : Instrument
    {
        #region Settings

        [EnabledIf(nameof(UseAutoDetection), false)]
        [Display("Serial Port Name", Order: 1, Description: "Example: 'COM3'")]
        public string SerialPortName { get; set; }

        [Display("Use AutoDetection", Order: 2, Group: "Serial Port AutoDetection", Collapsed: true)]
        public bool UseAutoDetection { get; set; } = false;

        [EnabledIf(nameof(UseAutoDetection))]
        [Display("USB Device Address", Order: 3, Group: "Serial Port AutoDetection", Collapsed: true,
            Description: "List of USB device addresses to search for a match.\n" +
                         @"Example: 'USB\VID_1234&PID_5678\SERIALNUMBER'")]
        public List<string> UsbDeviceAddresses { get; set; }

        [Display("Verbose Logging", Order: 20, Group: "Debug", Collapsed: true,
            Description: "Enables verbose logging of serial port (UART) communication.")]
        public bool VerboseLoggingEnabled { get; set; } = true;

        #endregion

        private SerialPort _sp;

        public override void Open()
        {
            base.Open();
            var portName = UseAutoDetection ? FindSerialPort() : SerialPortName;
            OpenSerialPort(portName);
        }

        private string FindSerialPort()
        {
            if (UsbDeviceAddresses.Count == 0)
                throw new InvalidOperationException(
                    "List of USB Device Address cannot be empty");

            if (VerboseLoggingEnabled)
                Log.Debug("Searching for USB Address(es) of " +
                          $"'{string.Join("', '", UsbDeviceAddresses)}'");

            var found = UsbSerialDevices.FindUsbAddress(UsbDeviceAddresses);

            Log.Debug($"Found serial port '{found.ComPort}' " +
                      $"with USB Address of '{found.UsbAddress}' " +
                      $"and Description of '{found.Description}'");

            return found.ComPort;
        }

        private void OpenSerialPort(string portName)
        {
            if (string.IsNullOrWhiteSpace(portName))
                throw new InvalidOperationException(
                    "Serial Port Name cannot be empty");

            _sp = new SerialPort
            {
                PortName = portName,
                BaudRate = 9600, // Not applicable
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.RequestToSend,
                ReadTimeout = 2000, // 2 second
                WriteTimeout = 2000, // 2 second
                DtrEnable = true,
                RtsEnable = true
            };

            // Close serial port if already opened
            CloseSerialPort();

            Log.Debug($"Opening serial port ({_sp.PortName})");

            // Open serial port
            _sp.Open();
            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
        }

        public override void Close()
        {
            CloseSerialPort();
            base.Close();
        }

        private void CloseSerialPort()
        {
            if (_sp.IsOpen)
            {
                Log.Debug($"Closing serial port ({_sp.PortName})");

                // Close serial port
                _sp.DiscardInBuffer();
                _sp.DiscardOutBuffer();
                _sp.Close();
                _sp.Dispose();
            }
        }

        public virtual string SerialQuery(string command)
        {
            SerialCommand(command);

            var response = string.Empty;
            const int timeoutMs = 3000;
            const int intervalMs = 10;
            const int maxCount = timeoutMs / intervalMs;
            var loopCount = 0;
            do
            {
                loopCount++;
                response += _sp.ReadExisting();
                TapThread.Sleep(intervalMs);
            } while (!response.Contains("\n") && loopCount < maxCount);

            if (VerboseLoggingEnabled)
                Log.Debug("{0} << {1}", _sp.PortName, response.Trim('\n'));

            return response;
        }

        public virtual void SerialCommand(string command)
        {
            if (VerboseLoggingEnabled)
                Log.Debug("{0} >> {1}", _sp.PortName, command);

            _sp.DiscardInBuffer();
            _sp.DiscardOutBuffer();
            _sp.Write(command);
        }
    }
}