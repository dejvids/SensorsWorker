using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace SensorsWorker
{
    class HttpWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly WorkerOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly Uri _baseAddress;
        private readonly Uri _endpoint;

        public HttpWorker(ILogger<HttpWorker> logger, IServiceProvider serviceProvider, IOptions<WorkerOptions> options)
        {
            _logger = logger;
            _options = options?.Value;
            _serviceProvider = serviceProvider;
            _baseAddress = new Uri(_options.BaseAddress);
            _endpoint = new Uri(_baseAddress, $"/sensors?api_key={_options.ApiKey}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var hub = scope.ServiceProvider.GetRequiredService<IHubContext<SensorsHub, ISensorsClient>>();

                var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
                string json;
                try
                {
                    using var result = await httpClient.GetAsync(_endpoint, stoppingToken);
                    result.EnsureSuccessStatusCode();
                    json = await result.Content.ReadAsStringAsync(stoppingToken);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    continue;
                }

                _logger.LogInformation(json);

                var data = JsonSerializer.Deserialize<SensorsDto>(json, new JsonSerializerOptions {PropertyNameCaseInsensitive = true });

                RoundData(data);

                await hub.Clients.All.RecieveDataAsync(data, stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(_options.Interval), stoppingToken);
            }
        }

        static void RoundData(SensorsDto data)
        {
            data.RoomTemp = Math.Round(data.RoomTemp, 1);
            data.WaterTemp = Math.Round(data.WaterTemp, 1);
            data.RoomPressure = Math.Round(data.RoomPressure, 1);
            data.RoomHumidity = Math.Round(data.RoomHumidity, 1);
        }
    }
}
