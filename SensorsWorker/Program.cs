using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using SensorsWorker.Extensions;
using System;
using Microsoft.Extensions.Configuration;

namespace SensorsWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    using ServiceProvider serviceProvider = services.BuildServiceProvider();
                    var configuration = serviceProvider.GetService<IConfiguration>();

                    
                    string baseAddress = configuration.GetSection("Worker")["BaseAddress"];
                    services.AddHttpClient();
                    services.AddSignalRCore();
                    services.AddWorkerOptions();
                   services.AddHostedService<HttpWorker>();
                })
            .ConfigureWebHostDefaults(webBuilder =>
                    webBuilder.ConfigureServices(services => {
                        services.AddCors(options =>
                        {
                            options.AddDefaultPolicy(builder =>
                            {
                                builder.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader();
                            });
                        });
                        services.AddSignalR();
                        })
                    
                    .Configure((context, app) =>
                    {
                        app.UseForwardedHeaders(new ForwardedHeadersOptions
                        {
                            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                        });
                        if (context.HostingEnvironment.IsDevelopment())
                        {
                            app.UseDeveloperExceptionPage();
                        }


                        app.UsePathBase("/signalr");
                        app.UseRouting();
                        app.UseCors();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/", async context => await context.Response.WriteAsync("signalR service"));
                            endpoints.MapGet("/error", async context => await context.Response.WriteAsync("error message"));
                            endpoints.MapHub<SensorsHub>("/sensorsHub");
                        });
                    })
                .UseKestrel(o => o.ListenAnyIP(5000))
                );
    }
}
