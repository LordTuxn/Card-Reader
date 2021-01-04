using CardReaderOne.Models;
using CardReaderOne.Outputs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CardReaderOne.Outputs.SignalR {

    public class SignalROutput : IDataProcess {
        private readonly IHubContext<SignalROutputHub, ISignalRClientOutput> hubOutput;

        public SignalROutput(IHubContext<SignalROutputHub, ISignalRClientOutput> hubOutput) {
            this.hubOutput = hubOutput;
        }

        public Task OutputAsync(EmployeeRecord employeeRecord) {
            hubOutput.Clients.All.OutputAsync(employeeRecord);
            return Task.CompletedTask;
        }
    }

    public class SignalROutputHub : Hub<ISignalRClientOutput> {
    }
}