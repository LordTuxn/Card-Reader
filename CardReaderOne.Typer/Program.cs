using CardReaderOne.DataProcess;
using CardReaderOne.Inputs;
using CardReaderOne.Inputs.HID;
using CardReaderOne.Inputs.Legic;
using CardReaderOne.Inputs.Mifare;
using CardReaderOne.Outputs;
using CardReaderOne.Outputs.SendKey;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace CardReaderOne.Typer {

    internal class Program {

        private static void Main(string[] args) {
            try {
                CreateHostBuilder(args).Build().RunAsync();

                Application.Run(new SystemTray());
            } catch (System.Exception ex) {
                Debug.WriteLine(ex.Message);
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) => {
                    //Hosted Services
                    services.AddHostedService<LegicInput>();
                    services.AddHostedService<MifareInput>();
                    services.AddHostedService<HIDInput>();

                    //Singleton Services
                    services.AddSingleton<IDataProcess, SendKeyOutput>();
                    services.AddSingleton<ICheckDevice, CheckDevice>();
                    services.AddSingleton<IDataProcessor, WebDataProcessor>();
                    services.AddSingleton<IDataProcessor, WebCachedDataProcessor>();

                    //Service Configurations
                    services.Configure<TyperOptions>(hostContext.Configuration.GetSection("OutputOptions"));
                    services.Configure<WebDataProcessOptions>(hostContext.Configuration.GetSection("DataProcessOptions"));
                    services.Configure<LegicOptions>(hostContext.Configuration.GetSection("InputOptions").GetSection("Legic"));
                    services.Configure<MifareOptions>(hostContext.Configuration.GetSection("InputOptions").GetSection("Mifare"));
                    services.Configure<HIDOptions>(hostContext.Configuration.GetSection("InputOptions").GetSection("HID"));
                }).ConfigureLogging(logging => {
                    //Logging Configuration
                    logging.AddConsole();
                    logging.AddDebug();
                }).ConfigureHostConfiguration(configHost => {
                    //Config Configuration
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddJsonFile("appsettings.json", optional: false);
                });
    }
}