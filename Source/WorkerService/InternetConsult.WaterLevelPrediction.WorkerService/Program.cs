#nullable enable
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InternetConsult.WaterLevelPrediction.WorkerService.Services;
using Serilog;
using System;

namespace InternetConsult.WaterLevelPrediction.WorkerService
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string? environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var appContextBaseDir = AppContext.BaseDirectory;
            var basePath = appContextBaseDir;

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true);

            configBuilder.AddEnvironmentVariables();
            var configuration = configBuilder.Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddMemoryCache();
                    services.AddTransient<ISunriseService, SunriseService>();
                });
    }
}
