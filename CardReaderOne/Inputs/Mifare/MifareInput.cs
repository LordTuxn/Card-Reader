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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace CardReaderOne.Inputs.Mifare {

    public class MifareInput : IHostedService, IDisposable {
        public readonly System.Timers.Timer ReadCardMIFARE = new System.Timers.Timer();

        private readonly ILogger logger;
        private readonly IOptions<MifareOptions> options;
        private readonly IDataProcessor dataProcessor;
        private readonly ICheckDevice checkDevice;

        private SerialPort port;

        public MifareInput(ILogger<MifareInput> logger, IOptions<MifareOptions> options, IDataProcessor dataProcessor, ICheckDevice checkDevice) {
            this.logger = logger;
            this.options = options;
            this.dataProcessor = dataProcessor;
            this.checkDevice = checkDevice;
        }

        public async Task StartAsync(CancellationToken cancellation) {
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
                    checkDevice.DeviceRemoved += DeviceRemoved;

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
                logger.LogCritical(ex, "An error occurred while starting MIFARE card reader");
            }

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellation) {
            logger.LogWarning("Card reader MIFARE stopped");
            await Task.CompletedTask;
        }

        private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e) {
            logger.LogError("An error occurred while receiving serial port");
        }

        //Receive data from card reader (SerialPort)
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e) {
            string CardNr = "";
            string OldMifareTag = "";

            try {
                for (int i = 0; i < 12; i++) {
                    CardNr += port.ReadExisting();

                    //End at \r
                    if (CardNr.Contains(char.Parse("\r"))) {
                        i = 12;
                    }
                    Thread.Sleep(50);
                }

                if (CardNr.Length > 3) {
                    CardNr = CardNr.Replace("00\r", "");
                }
                if (OldMifareTag.Length > 3) {
                    OldMifareTag = OldMifareTag.Replace("00\r", "");
                }

                if (OldMifareTag != CardNr) {
                    if(port.IsOpen) {
                        port.Write("040201" + Environment.NewLine);

                        OldMifareTag = CardNr;

                        //Check if card number length is correct and convert it
                        if (CardNr.Length > 5) {
                            CardNr = CardNr.Replace("\r", "");
                            CardNr = CardNr.Replace("02050384", "");
                            CardNr = CardNr.Replace("0001803807", "");
                            CardNr = Convert.ToInt64("0x" + CardNr, 16).ToString();
                            CardNr = CardNr.Substring(8, 8);
                        }

                        //Check if card number length is correct
                        if (CardNr.Length == 8) {
                            port.DiscardInBuffer();

                            logger.LogInformation("Successfully received data from MIFARE card reader: {cardNr}", CardNr);

                            dataProcessor.ProcessAsync(new EmployeeRecord(CardNr));
                        } else {
                            //logger.LogError("Card number MIFARE is invalid");
                        }
                    }
                }
            } catch (Exception ex) {
                logger.LogCritical(ex, "An error occurred while receiving data from card reader MIFARE");
            }
        }

        /// <summary>
        /// Connect card reader MIFARE
        /// </summary>
        private void AddDevice(CheckDeviceArgs e) {
            try {
                if (e.COMPort == options.Value.COMPort) {
                    port = new SerialPort(options.Value.COMPort, options.Value.COMPaud, Parity.None, 8, StopBits.One);
                    port.DataReceived += Port_DataReceived;
                    port.ErrorReceived += Port_ErrorReceived;
                    port.Open();

                    //Enable green light
                    port.Write("040202" + Environment.NewLine);

                    if (options.Value.CardReadingSpeed <= 0) {
                        logger.LogWarning("ReadSpeed is invalid");
                        options.Value.CardReadingSpeed = 1000;
                    }

                    ReadCardMIFARE.Interval = options.Value.CardReadingSpeed;
                    ReadCardMIFARE.Elapsed += ReadCardMIFARE_Elapsed;
                    ReadCardMIFARE.Start();

                    logger.LogInformation("Successfully opened serial port for MIFARE");
                }
            } catch (UnauthorizedAccessException) {
                logger.LogError("No access on {options.Value.COMPort} input", options.Value.COMPort);
            } catch (System.IO.IOException) {
                logger.LogError("Could not find any device on {options.Value.COMPort}", options.Value.COMPort);
            } catch (Exception ex) {
                logger.LogError(ex, "An error occurred while connecting to card reader MIFARE");
            }
        }

        //USB device attached event
        private void DeviceAttached(object sender, CheckDeviceArgs e) {
            AddDevice(e);
        }

        private void DeviceRemoved(object sender, CheckDeviceArgs e) {
            try {
                if (e.USBVID == options.Value.USBVID && e.USBPID == options.Value.USBPID) {
                    port.Close();
                    ReadCardMIFARE.Elapsed -= ReadCardMIFARE_Elapsed;
                }
            } catch (Exception) {
                logger.LogError("Could not close MIFARE port");
            }
        }

        //Remove event when class get disposed
        public void Dispose() {
            checkDevice.DeviceAttached -= DeviceAttached;
            checkDevice.DeviceRemoved -= DeviceRemoved;
        }

        //Read Card Timer
        private void ReadCardMIFARE_Elapsed(object sender, ElapsedEventArgs e) {
            try {
                if (port.IsOpen) {
                    logger.LogTrace("Request data from MIFARE");

                    //Read card
                    port.Write("050010" + Environment.NewLine);
                }
            } catch (Exception ex) {
                logger.LogError(ex, "An error occurred while writing to card reader MIFARE");
            }
        }
    }
}