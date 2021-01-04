using CardReaderOne.DataProcess;
using CardReaderOne.Models;
using CardReaderOne.Outputs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CardReaderOne.Inputs.Legic {

    public class LegicInput : IHostedService, IDisposable {
        private readonly ILogger logger;
        private readonly IOptions<LegicOptions> options;
        private readonly IDataProcessor dataProcessor;
        private readonly ICheckDevice checkDevice;

        private SerialPort port;

        public LegicInput(ILogger<LegicInput> logger, IOptions<LegicOptions> options, IDataProcessor dataProcessor, ICheckDevice checkDevice) {
            this.logger = logger;
            this.options = options;
            this.dataProcessor = dataProcessor;
            this.checkDevice = checkDevice;
        }

        public async Task StartAsync(CancellationToken cancellation) {
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

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellation) {
            logger.LogWarning("Card reader LEGIC stopped");
            await Task.CompletedTask;
        }

        private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e) {
            logger.LogError("An error occurred while receiving serial port");
        }

        //Receive data from card reader (SerialPort)
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e) {
            try {
                if (port != null && port.IsOpen) {
                    string cardNr = string.Empty;

                    for (int i = 0; i < 12; i++) {
                        cardNr += port.ReadExisting();

                        //End at @ and remove it
                        if (cardNr.Contains(char.Parse("@"))) {
                            i = 12;
                            cardNr = cardNr.Replace("@", "");
                        }
                        Thread.Sleep(50);
                    }

                    //Check if card number length is correct and convert it
                    if (cardNr.Length == 14) {
                        cardNr = cardNr.Substring(9, 5);

                        logger.LogInformation("Successfully received data from LEGIC card reader: {cardNr}", cardNr);

                        dataProcessor.ProcessAsync(new EmployeeRecord(cardNr));
                    } else {
                        logger.LogError("Card reader LEGIC is invalid");
                    }
                }
            } catch (Exception ex) {
                logger.LogCritical(ex, "An error occurred while receiving data from card reader LEGIC");
            }
        }

        /// <summary>
        /// Connect card reader LEGIC
        /// </summary>
        private void AddDevice(CheckDeviceArgs e) {
            try {
                if (e.COMPort == options.Value.COMPort) {
                    port = new SerialPort(options.Value.COMPort, options.Value.COMPaud, Parity.None, 8, StopBits.One);
                    port.DataReceived += Port_DataReceived;
                    port.ErrorReceived += Port_ErrorReceived;
                    port.Open();

                    logger.LogInformation("Successfully opened serial port for LEGIC");
                }
            } catch (UnauthorizedAccessException) {
                logger.LogError("No access on {options.Value.COMPort} input", options.Value.COMPort);
            } catch (System.IO.IOException) {
                logger.LogError("Could not find any device on {options.Value.COMPort}", options.Value.COMPort);
            } catch (Exception ex) {
                logger.LogError(ex, "An error occurred while connecting to card reader LEGIC");
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