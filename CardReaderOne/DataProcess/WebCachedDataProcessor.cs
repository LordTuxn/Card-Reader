using CardReaderOne.Models;
using CardReaderOne.Outputs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CardReaderOne.DataProcess {

    public class WebCachedDataProcessor : WebDataProcessor {
        private readonly ILogger<WebCachedDataProcessor> logger;
        private readonly Dictionary<string, EmployeeRecord> cache = new Dictionary<string, EmployeeRecord>();

        public WebCachedDataProcessor(ILogger<WebCachedDataProcessor> logger, ILogger<WebDataProcessor> baseLogger, IOptions<WebDataProcessOptions> options, IDataProcess output) : base(baseLogger, options, output) {
            this.logger = logger;
        }

        public override async Task ProcessAsync(EmployeeRecord employeeRecord) {
            try {
                await base.ProcessAsync(employeeRecord);
                cache[employeeRecord.CardNumber] = employeeRecord;
            } catch (Exception) {
                cache.TryGetValue(employeeRecord.CardNumber, out EmployeeRecord cachedRecord);

                if (cachedRecord != null) {
                    employeeRecord.DisplayName = cachedRecord.DisplayName;
                    employeeRecord.EMail = cachedRecord.EMail;
                    employeeRecord.EmployeeNumber = cachedRecord.EmployeeNumber;
                    employeeRecord.Firstname = cachedRecord.Firstname;
                    employeeRecord.Lastname = cachedRecord.Lastname;
                    employeeRecord.ValidFrom = cachedRecord.ValidFrom;
                    employeeRecord.ValidTo = cachedRecord.ValidTo;

                    if (DateTime.Now >= employeeRecord.ValidTo || DateTime.Now <= employeeRecord.ValidFrom) {
                        logger.LogError("Card is invalid");
                        return;
                    } else {
                        await output.OutputAsync(employeeRecord);
                    }
                } else {
                    logger.LogCritical("Could not receive employee data using http request or web cache.");
                }
            }
        }
    }
}