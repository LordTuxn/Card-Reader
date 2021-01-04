using CardReaderOne.Models;
using CardReaderOne.Outputs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CardReaderOne.DataProcess {
    public class NullDataProcessor : IDataProcessor {

        public NullDataProcessor(ILogger<NullDataProcessor> logger, IDataProcess output) {
            Logger = logger;
            Output = output;
        }

        public ILogger<NullDataProcessor> Logger { get; }
        public IDataProcess Output { get; }

        public Task ProcessAsync(EmployeeRecord employee) {
            return Output.OutputAsync(employee);
        }
    }
}
