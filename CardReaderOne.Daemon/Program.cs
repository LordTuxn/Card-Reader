using CardReaderOne.DataProcess;
using CardReaderOne.Inputs;
using CardReaderOne.Inputs.HID;
using CardReaderOne.Inputs.Legic;
using CardReaderOne.Inputs.Mifare;
using CardReaderOne.Outputs;
using CardReaderOne.Outputs.SignalR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CardReaderOne.Daemon {

    public class Program {

        public static void Main(string[] args) {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureServices((hostContext, services) => {
                    //Singleton Services
                    services.AddSingleton<IDataProcess, SignalROutput>();
                    services.AddSingleton<ICheckDevice, CheckDevice>();
                    services.AddSingleton<IDataProcessor, WebDataProcessor>();
                    services.AddSingleton<IDataProcessor, WebCachedDataProcessor>();

                    //Hosted Services
                    services.AddHostedService<LegicInput>();
                    services.AddHostedService<MifareInput>();
                    services.AddHostedService<HIDInput>();

                    //Service Configurations
                    services.Configure<WebDataProcessOptions>(hostContext.Configuration.GetSection("DataProcessOptions"));
                    services.Configure<LegicOptions>(hostContext.Configuration.GetSection("InputOptions").GetSection("Legic"));
                    services.Configure<MifareOptions>(hostContext.Configuration.GetSection("InputOptions").GetSection("Mifare"));
                    services.Configure<HIDOptions>(hostContext.Configuration.GetSection("InputOptions").GetSection("HID"));
                });
    }
}