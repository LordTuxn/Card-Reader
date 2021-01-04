using CardReaderOne.Models;
using CardReaderOne.Outputs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CardReaderOne.Inputs.HID {

    public class HIDInput : IHostedService, IDisposable {
        public bool connected = false;
        private readonly ILogger logger;
        private readonly IOptions<HIDOptions> options;
        private readonly DataProcess.IDataProcessor output;
        private readonly ICheckDevice checkDevice;

        private SerialPort port;

        public HIDInput(ILogger<HIDInput> logger, IOptions<HIDOptions> options, DataProcess.IDataProcessor output, ICheckDevice checkDevice) {
            this.logger = logger;
            this.options = options;
            this.output = output;
            this.checkDevice = checkDevice;
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            try {
                if (string.IsNullOrEmpty(options.Value.COMPort)) {
                    logger.LogWarning("COMPort is null");
                    return;
                }

                if (options.Value.COMPaud == 0) {
                    logger.LogWarning("COMPaud is null");
                    return;
                }

                if (string.IsNullOrEmpty(options.Value.USBPID)) {
                    logger.LogWarning("USBPID is null");
                    return;
                }

                if (string.IsNullOrEmpty(options.Value.USBVID)) {
                    logger.LogWarning("USBVID is null");
                    return;
                }

                try {
                    //Initialize USB attach and remove events for card reader
                    checkDevice.DeviceAttached += DeviceAttached;

                    var e = new CheckDeviceArgs() {
                        COMPort = options.Value.COMPort,
                        USBPID = options.Value.USBPID,
                        USBVID = options.Value.USBVID,
                    };

                    AddDevice(e);
                } catch (Exception ex) {
                    logger.LogError(ex, "An error occurred while initializing USB attach and remove events");
                }
            } catch (Exception ex) {
                logger.LogCritical(ex, "An error occurred while starting HID card reader");
            }

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken) {
            logger.LogWarning("Card reader HID stopped");
            await Task.CompletedTask;
        }

        private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e) {
            logger.LogError("An error occurred while receiving the serial port");
        }

        //Receive data from card reader (SerialPort)
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e) {
            int datalength = 0;
            string bindata = string.Empty;
            string cardNr = string.Empty;

            try {
                for (int i = 0; i < 12; i++) {
                    cardNr += port.ReadExisting();

                    if (cardNr.Contains(char.Parse("\r"))) {
                        i = 12;
                    }
                    Thread.Sleep(50);
                }

                if (cardNr.Length > 2) {
                    cardNr = cardNr.Replace("\r", "");
                    datalength = int.Parse(cardNr.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    bindata = Convert.ToString(Convert.ToInt32(cardNr.Substring(2, cardNr.Length - 2), 16), 2).Substring(0, datalength - 1);
                    cardNr = Convert.ToInt64(bindata.Substring(8, bindata.Length - 8), 2).ToString();
                }

                if (cardNr.Length > 3) {
                    cardNr = cardNr.Replace("\r", "");

                    if (datalength == 26) {
                        string data = cardNr.ToString();

                        string Parity1 = string.Empty;
                        string Parity2 = string.Empty;
                        if (bindata.Length > 12) {
                            int bitcount = 0;

                            foreach (char itm in bindata.Substring(1, 12).ToCharArray()) {
                                if (itm.ToString() == "1") {
                                    bitcount++;
                                }
                            }

                            if (bitcount % 2 == 0) {
                                Parity1 = "0";
                            } else {
                                Parity1 = "1";
                            }

                            bitcount = 0;
                            foreach (char itm in bindata.Substring(bindata.Length - 13, 12).ToCharArray()) {
                                if (itm.ToString() == "1") {
                                    bitcount++;
                                }
                            }

                            if (bitcount % 2 == 0) {
                                Parity2 = "1";
                            } else {
                                Parity2 = "0";
                            }
                        }

                        if (bindata.Substring(0, 1) == Parity1 && bindata.Substring(bindata.Length - 1, 1) == Parity2 || true) {
                            cardNr = Convert.ToInt64(bindata.Substring(9, 16), 2).ToString();

                            logger.LogInformation("Successfully received data from HID card reader: {cardNr}", cardNr);

                            output.ProcessAsync(new EmployeeRecord(cardNr));
                        } else {
                            logger.LogError("Card number HID is invalid");
                        }
                    }
                }
            } catch (Exception ex) {
                logger.LogError(ex, "An error occurred while receiving data from HID card reader");
            }
        }

        /// <summary>
        /// Connect disconnect card reader HID
        /// </summary>
        private void AddDevice(CheckDeviceArgs e) {
            try {
                if (e.COMPort == options.Value.COMPort) {
                    port = new SerialPort(options.Value.COMPort, options.Value.COMPaud, Parity.None, 8, StopBits.One);
                    port.DataReceived += Port_DataReceived;
                    port.ErrorReceived += Port_ErrorReceived;
                    port.Open();

                    logger.LogInformation("Successfully opened serial port for HID");
                }
            } catch (UnauthorizedAccessException) {
                logger.LogError("No access on {options.Value.COMPort} input", options.Value.COMPort);
            } catch (System.IO.IOException) {
                logger.LogError("Could not find any device on {options.Value.COMPort}", options.Value.COMPort);
            } catch (Exception ex) {
                logger.LogError(ex, "An error occurred while connecting to card reader HID");
            }
        }

        //USB device attached event
        private void DeviceAttached(object sender, CheckDeviceArgs e) {
            AddDevice(e);
        }

        //Remove event when class get disposed
        public void Dispose() {
            checkDevice.DeviceAttached -= DeviceAttached;
        }
    }
}