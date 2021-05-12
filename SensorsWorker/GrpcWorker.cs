using Grpc.Net.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SensorsClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SensorsWorker
{
    public class GrpcWorker : BackgroundService
    {
        private readonly ILogger<GrpcWorker> _logger;

        public GrpcWorker(ILogger<GrpcWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var rand = new Random();
            while (!stoppingToken.IsCancellationRequested)
            {
                // The port number(5001) must match the port of the gRPC server.
                try
                {

                using var channel = GrpcChannel.ForAddress("http://localhost:5000");
                var client = new Sensors.SensorsClient(channel);
                var req = new UpdateRequest
                {
                    Preassure = rand.NextDouble() * 1000,
                    Humidity = rand.NextDouble() * 1000,
                    Temperature = rand.NextDouble() * 1000
                };
                client.UpdateSensors(req);
                Console.WriteLine("Updated");

                await channel.ShutdownAsync();
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "worker error");
                }

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
