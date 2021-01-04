using CardReaderOne.Models;
using CardReaderOne.Outputs;
using CardReaderOne.Outputs.SendKey;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CardReaderOne.Outputs.SendKey {

    public class SendKeyOutput : IDataProcess {
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int cmd);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly ILogger logger;
        private readonly IOptions<TyperOptions> options;

        public SendKeyOutput(ILogger<SendKeyOutput> logger, IOptions<TyperOptions> options) {
            this.logger = logger;
            this.options = options;

            if (string.IsNullOrEmpty(options.Value.TypeSequence)) {
                logger.LogError("TypeSequence is null");
                return;
            }

            if (string.IsNullOrEmpty(options.Value.ProcessName)) {
                logger.LogError("ProcessName is null");
                return;
            }
        }

        /// <summary>
        /// send data to the application window
        /// </summary>
        /// <param name="employeeRecord"></param>
        /// <returns></returns>
        public Task OutputAsync(EmployeeRecord employeeRecord) {
            try {
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();

                IntPtr handle = IntPtr.Zero;
                try {
                    var process = Process.GetProcessesByName(options.Value.ProcessName).FirstOrDefault();
                    logger.LogTrace("Found process {process}", process);
                    if (process != null) {
                        handle = process.MainWindowHandle;
                    } else {
                        logger.LogError("Could not find process window");
                    }
                } catch (Exception ex) {
                    logger.LogCritical(ex, "An error occured while focusing the receiver window");
                }

                //focus window
                if (handle != IntPtr.Zero) {
                    ShowWindow(handle, SW_RESTORE);
                    SetForegroundWindow(handle);
                    logger.LogTrace("Successfully focused console window");

                    //send data to output window
                    shell.SendKeys(options.Value.TypeSequence
                        .Replace("{CardNumber}", CleanTypeSequence(employeeRecord.CardNumber))
                        .Replace("{DisplayName}", CleanTypeSequence(employeeRecord.DisplayName))
                        .Replace("{EMail}", CleanTypeSequence(employeeRecord.EMail))
                        .Replace("{Firstname}", CleanTypeSequence(employeeRecord.Firstname))
                        .Replace("{Lastname}", CleanTypeSequence(employeeRecord.Lastname))
                        .Replace("{EmployeeNumber}", CleanTypeSequence(employeeRecord.EmployeeNumber.ToString()))
                        .Replace("{ValidFrom}", CleanTypeSequence(employeeRecord.ValidFrom.ToShortDateString()))
                        .Replace("{ValidTo}", CleanTypeSequence(employeeRecord.ValidTo.ToShortDateString())));

                    logger.LogInformation("Successfully sent data to the application");
                }
            } catch (Exception ex) {
                logger.LogError(ex, "An error occurred while sending data to the application");
            }

            return Task.CompletedTask;
        }

        private string CleanTypeSequence(string sequence) {
            return Regex.Replace(sequence, @"[\+\^~%\(\)\{\}\[\]]", "{$&}", RegexOptions.IgnoreCase);
        }
    }
}