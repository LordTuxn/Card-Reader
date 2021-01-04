using CardReaderOne.Models;
using CardReaderOne.Outputs.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace CardReaderOne.SignalRClient {

    internal class SignalRConsoleOutput : SignalRClientOutput {

        public SignalRConsoleOutput(ILogger<SignalRClientOutput> logger, IOptions<SignalRClientOptions> options) : base(logger, options) {
        }

        public override Task OutputAsync(EmployeeRecord employeeRecord) {
            Console.WriteLine(employeeRecord.Firstname + " " + employeeRecord.Lastname);
            return Task.CompletedTask;
        }
    }
}