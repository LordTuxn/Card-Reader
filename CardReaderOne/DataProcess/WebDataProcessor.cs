using CardReaderOne.Models;
using CardReaderOne.Outputs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace CardReaderOne.DataProcess {

    public class WebDataProcessor : IDataProcessor {
        private readonly ILogger<WebDataProcessor> logger;
        private readonly IOptions<WebDataProcessOptions> options;
        protected readonly IDataProcess output;

        private readonly HttpClient client = new HttpClient();

        public WebDataProcessor(ILogger<WebDataProcessor> logger, IOptions<WebDataProcessOptions> options, IDataProcess output) {
            this.logger = logger;
            this.options = options;
            this.output = output;
        }

        public virtual async Task ProcessAsync(EmployeeRecord employeeRecord) {
            try {
                if (string.IsNullOrEmpty(options.Value.URL)) {
                    logger.LogError("URL is null");
                    return;
                }

                if (string.IsNullOrEmpty(options.Value.Username)) {
                    logger.LogError("Username is null");
                    return;
                }

                if (string.IsNullOrEmpty(options.Value.Password)) {
                    logger.LogError("Password is null");
                    return;
                }

                var result = await GetEmployeeData(employeeRecord.CardNumber);
                if (result != null) {
                    employeeRecord.DisplayName = result.DisplayName;
                    employeeRecord.EMail = result.EMail;
                    employeeRecord.EmployeeNumber = result.EmployeeNumber;
                    employeeRecord.Firstname = result.Firstname;
                    employeeRecord.Lastname = result.Lastname;
                    employeeRecord.ValidFrom = result.ValidFrom;
                    employeeRecord.ValidTo = result.ValidTo;

                    if (DateTime.Now >= employeeRecord.ValidTo || DateTime.Now <= employeeRecord.ValidFrom) {
                        logger.LogError("Card is invalid");
                        return;
                    }

                    await output.OutputAsync(employeeRecord);
                } else {
                    throw new NullReferenceException("Result is null");
                }
            } catch (Exception ex) {
                logger.LogError(ex, "An error occurred while processing employee data");
            }
        }

        protected async Task<EmployeeRecord> GetEmployeeData(string cardNr) {
            try {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{options.Value.Username}:{options.Value.Password}")));
                HttpResponseMessage response = await client.GetAsync(options.Value.URL + cardNr);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<EmployeeRecord>();
            } catch (Exception ex) {
                logger.LogError(ex, "An error occurred while receiving employee data using http request");
                return null;
            }
        }
    }
}