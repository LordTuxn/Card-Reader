using CardReaderOne.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace CardReaderOne.Outputs {

    public interface IDataProcess {

        Task OutputAsync(EmployeeRecord employeeRecord);
    }
}