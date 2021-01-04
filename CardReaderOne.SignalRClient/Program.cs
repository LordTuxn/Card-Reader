using CardReaderOne.Outputs.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace CardReaderOne.SignalRClient {

    internal class Program {

        private static void Main(string[] args) {
            var container = CreateHostBuilder(args).Build();
            container.RunAsync();

            container.Services.GetRequiredService<SignalRConsoleOutput>();

            Console.ReadKey();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) => {
                    //Singleton Services
                    services.AddSingleton<SignalRConsoleOutput>();

                    //Service Configurations
                    services.Configure<SignalRClientOptions>(hostContext.Configuration.GetSection("SignalROutput"));
                });
    }
}