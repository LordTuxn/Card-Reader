using CardReaderOne.Models;
using CardReaderOne.Outputs.SendKey;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CardReaderOne.Outputs.Socket {

    public class SocketOutputClient : IDataProcess {
        private readonly ILogger logger;

        public SocketOutputClient(ILogger<SocketOutputClient> logger) {
            this.logger = logger;
        }

        private static TcpClient client = null;

        public Task StartSocketServer() {
            //connect to server
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 8888);
            server.Start();
            logger.LogInformation("Started the socket server");
            client = server.AcceptTcpClient();
            logger.LogInformation("Accept connection from the client");

            while (true) {
                try {
                    if (client.Connected) {
                        NetworkStream networkStream = client.GetStream();
                        byte[] bytesFrom = new byte[125000];
                        networkStream.Read(bytesFrom, 0, client.ReceiveBufferSize);
                        string dataFromClient = Encoding.ASCII.GetString(bytesFrom);
                        if (dataFromClient.Length > 0 & dataFromClient.Contains("$")) {
                            dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));
                            logger.LogInformation($"Successfully received data - {dataFromClient}");
                        }
                    }
                } catch (Exception ex) {
                    logger.LogError(ex, "An error occurred while receiving data from the client");
                    return Task.CompletedTask;
                }
            }
        }

        public Task OutputAsync(EmployeeRecord employeeRecord) {
            return Task.CompletedTask;
        }
    }
}