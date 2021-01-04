using CardReaderOne.Inputs.HID;
using CardReaderOne.Inputs.Legic;
using CardReaderOne.Inputs.Mifare;
using CardReaderOne.Outputs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Usb.Events;

namespace CardReaderOne.Inputs {

    public class CheckDevice : ICheckDevice {
        private readonly ILogger logger;

        private readonly IUsbEventWatcher usbEventWatcher = new UsbEventWatcher();

        public event EventHandler<CheckDeviceArgs> DeviceAttached;
        public event EventHandler<CheckDeviceArgs> DeviceRemoved;

        public CheckDevice(ILogger<CheckDevice> logger) {
            this.logger = logger;

            //Initialize USB attach event
            usbEventWatcher.UsbDeviceAdded += USBDeviceAdded;
            usbEventWatcher.UsbDeviceRemoved += USBDeviceRemoved;
        }

        //Get COMPort using USBVID and USBPID
        private string GetComPort(string VID, string PID) {
            string pattern = string.Format("^VID_{0}.PID_{1}", VID, PID);
            Regex _rx = new Regex(pattern, RegexOptions.IgnoreCase);
            RegistryKey rk1 = Registry.LocalMachine;
            RegistryKey rk2 = rk1.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum");
            foreach (string s3 in rk2.GetSubKeyNames()) {
                RegistryKey rk3 = rk2.OpenSubKey(s3);
                foreach (string s in rk3.GetSubKeyNames()) {
                    if (_rx.Match(s).Success) {
                        RegistryKey rk4 = rk3.OpenSubKey(s);
                        foreach (string s2 in rk4.GetSubKeyNames()) {
                            RegistryKey rk5 = rk4.OpenSubKey(s2);
                            RegistryKey rk6 = rk5.OpenSubKey("Device Parameters");
                            return (string)rk6.GetValue("PortName");
                        }
                    }
                }
            }
            return null;
        }

        //USB device attached event
        private void USBDeviceAdded(object sender, UsbDevice e) {
            try {
                logger.LogTrace("[Event] USB device attached");

                var d = new CheckDeviceArgs {
                    COMPort = GetComPort(e.VendorID, e.ProductID),
                    USBPID = e.ProductID,
                    USBVID = e.VendorID,
                   
                };

                //Call event 
                DeviceAttached?.Invoke(this, d);
            } catch (Exception ex) {
                logger.LogError(ex, "An error occurred while calling the USB device attached event");
            }
        }

        //USB device removed event
        private void USBDeviceRemoved(object sender, UsbDevice e) {
            try {
                logger.LogTrace("[Event] USB device removed");

                var d = new CheckDeviceArgs {
                    USBPID = e.ProductID,
                    USBVID = e.VendorID,
                };

                //Call event 
                DeviceRemoved?.Invoke(this, d);
            } catch (Exception ex) {
                logger.LogError(ex, "An error occurred while calling the USB device removed event");
            }
        }
    }

    public class CheckDeviceArgs : EventArgs {
        public string COMPort { get; set; }

        public string USBPID { get; set; }

        public string USBVID { get; set; }
    }
}