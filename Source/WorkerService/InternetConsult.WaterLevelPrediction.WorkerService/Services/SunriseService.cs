using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace InternetConsult.WaterLevelPrediction.WorkerService.Services
{
    public class SunriseService : ISunriseService
    {
        private readonly ILogger<SunriseService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;

        private string _sunriseBaseUrl;
        private int _sunriseCaseSecs;
        private string _latitude;
        private string _longitude;

        public SunriseService(IConfiguration configuration, ILogger<SunriseService> logger, IMemoryCache memoryCache)
        {
            _configuration = configuration;
            _logger = logger;
            _memoryCache = memoryCache;
            _sunriseBaseUrl = _configuration.GetValue<string>("SunriseBaseUrl");
            _sunriseCaseSecs = _configuration.GetValue<int>("SunriseCacheSecs");
            _latitude = _configuration.GetValue<string>("SunriseLatitude");
            _longitude = _configuration.GetValue<string>("SunriseLongitude");
        }

        public async Task<Sunrise> GetSunriseResult()
        {
            var sunriseResult = _memoryCache.GetOrCreate("CacheTime", entry =>
            {
                _logger.LogInformation("No cache hit. Fetching sunrise data from the service.");
                entry.SlidingExpiration = TimeSpan.FromSeconds(_sunriseCaseSecs);
                return GetSunriseResult2(_latitude, _longitude);
            });
            return await sunriseResult;
        }

        private async Task<Sunrise> GetSunriseResult2(string latitude, string longitude)
        {
            using (var client = new HttpClient())
            {
                var payload = await client.GetStringAsync(
                    $"{_sunriseBaseUrl}/json?lat={latitude}&lng={longitude}&formatted=0");

                var sunrise = JsonConvert.DeserializeObject<Sunrise>(payload);
                return sunrise;
            }
        }
    }
}
