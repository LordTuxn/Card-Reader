using CardReaderOne.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CardReaderOne.Outputs.SignalR {

    public abstract class SignalRClientOutput {

        public SignalRClientOutput(ILogger<SignalRClientOutput> logger, IOptions<SignalRClientOptions> options) {
            try {
                if (string.IsNullOrEmpty(options.Value.ServerURL)) {
                    logger.LogError("ServerURL is null");
                    return;
                }

                if (string.IsNullOrEmpty(options.Value.TypeSequence)) {
                    logger.LogError("TypeSequence is null");
                    return;
                }

                HubConnection connection = new HubConnectionBuilder()
               .WithUrl(options.Value.ServerURL)
               .Build();

                connection.Closed += async (error) => {
                    await Task.Delay(1000);
                    await connection.StartAsync();
                };

                connection.On<EmployeeRecord>("OutputAsync", (employeeRecord) => {
                    logger.LogInformation("Successfully sent data to the application: {employeeRecord}", employeeRecord);

                    OutputAsync(employeeRecord);
                });

                try {
                    connection.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    logger.LogInformation("Successfully connected to the client");
                } catch (Exception ex) {
                    logger.LogError(ex, "An error occurred while connecting to the client");
                }
            } catch (Exception ex) {
                logger.LogError(ex, "An error occurred while initializing the connection to the client");
            }
        }

        public abstract Task OutputAsync(EmployeeRecord employeeRecord);
    }
}