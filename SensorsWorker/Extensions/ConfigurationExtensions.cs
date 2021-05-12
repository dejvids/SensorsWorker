using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SensorsWorker.Extensions
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddWorkerOptions(this IServiceCollection services)
        {
            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetService<IConfiguration>();

            services.Configure<WorkerOptions>(options => configuration.GetSection("Worker").Bind(options));

            return services;
        }
    }
}
