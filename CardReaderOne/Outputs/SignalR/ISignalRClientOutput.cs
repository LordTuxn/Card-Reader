using CardReaderOne.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CardReaderOne.Outputs.SignalR {

    public interface ISignalRClientOutput {

        Task OutputAsync(EmployeeRecord employeeRecord);
    }
}