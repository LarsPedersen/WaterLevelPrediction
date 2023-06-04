#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InternetConsult.WaterLevelPrediction.Model;
using InternetConsult.WaterLevelPrediction.WorkerService.Services;
using Newtonsoft.Json;

namespace InternetConsult.WaterLevelPrediction.WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _predictionApiBaseUrl;
        private readonly string _predictionKey;
        private readonly string _photoWebServerBaseUrl;
        private readonly int _workerDelaySecs;
        private readonly string _tempFolderPath;
        private readonly int _twilightThresholdMinutes;
        private readonly ISunriseService _sunriseService;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IMemoryCache memoryCache, ISunriseService sunriseService)
        {
            _logger = logger;
            _configuration = configuration;
            _predictionApiBaseUrl = _configuration.GetValue<string>("PredictionApiBaseUrl");
            _predictionKey = _configuration.GetValue<string>("PredictionKey");
            _photoWebServerBaseUrl = _configuration.GetValue<string>("PhotoWebServerBaseUrl");
            _workerDelaySecs = _configuration.GetValue<int>("WorkerDelaySecs");
            _tempFolderPath = _configuration.GetValue<string>("TempFolderPath");
            _sunriseService = sunriseService;
            _twilightThresholdMinutes = _configuration.GetValue<int>("TwilightThresholdMinutes");
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var workerDelaySecs = _workerDelaySecs;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                    byte[] imageBytes = await GetImage(cancellationToken);

                    var predictionResults = await PredictWaterLevel(imageBytes);
                    var maxPredictionResults = predictionResults.Predictions.OrderByDescending(c => c.Probability).FirstOrDefault();
                    var tagName = maxPredictionResults.TagName.Replace("%", "");


                    StoreImage(imageBytes, tagName, cancellationToken);
                    PublishPredictionResults(predictionResults, tagName, cancellationToken);
                                        
                    bool isNight = false;
                    // twilightThresholdMinutes is used to control how much before twilight end the night mode should start and how much after twilight begin 
                    // the night mode should end.
                    var twilightThresholdMinutes = _twilightThresholdMinutes;

                    var sunriseResult = _sunriseService.GetSunriseResult();
                    if (sunriseResult.Result.Status == "OK")
                    {
                        var dateTimeUtcNow = DateTime.UtcNow;
                        var civilTwilightBegin = sunriseResult.Result.Results.CivilTwilightBegin;
                        var civilTwilightEnd = sunriseResult.Result.Results.CivilTwilightEnd;

                        isNight = (dateTimeUtcNow > (civilTwilightEnd.AddMinutes(-twilightThresholdMinutes)) || dateTimeUtcNow < civilTwilightBegin.AddMinutes(twilightThresholdMinutes));
                        // Delay until sunrise.
                        if (isNight)
                        {
                            workerDelaySecs = Convert.ToInt32(CalculateDelayUntilSunrise(dateTimeUtcNow, civilTwilightBegin, twilightThresholdMinutes));
                        }
                    }
                    await Task.Delay(workerDelaySecs * 1000, cancellationToken);
                    // Back to the configured delay.
                    workerDelaySecs = _workerDelaySecs;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "And error occurred executing work.");
                }
            }
        }

        double CalculateDelayUntilSunrise(DateTime dateTimeUtcNow, DateTimeOffset civilTwilightBegin, int sunriseThresholdMinutes)
        {
            TimeSpan nightDelay;
            if (dateTimeUtcNow < civilTwilightBegin)
            {
                nightDelay = dateTimeUtcNow - civilTwilightBegin.AddMinutes(sunriseThresholdMinutes);
            }
            else
            {
                nightDelay = (civilTwilightBegin.AddDays(1).AddMinutes(sunriseThresholdMinutes) - dateTimeUtcNow);
            }
            _logger.LogInformation($"Night delay pause time span '{nightDelay.Hours}:{nightDelay.Minutes}:{nightDelay.Minutes}'.");
            return nightDelay.TotalSeconds;
        }

        private async void PublishPredictionResults(PredictionResults predictionResults, string tagName, CancellationToken cancellationToken)
        {
            var destinationPath = Path.Combine(_tempFolderPath, tagName);
            var dirInfo = Directory.CreateDirectory(destinationPath);
            var fileName = "PredictionResults.json";
            string filePath = GetFilePath(dirInfo, fileName); 
            await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(predictionResults), cancellationToken);
            _logger.LogDebug($"PredictionResults file stored at: '{filePath}'");
        }

        private async void StoreImage(byte[] imageBytes, string tagName, CancellationToken cancellationToken)
        {
            var destinationPath = Path.Combine(_tempFolderPath, tagName);
            var dirInfo = Directory.CreateDirectory(destinationPath);
            var fileName = "Tank.jpg";
            string filePath = GetFilePath(dirInfo, fileName); 
            await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);
            _logger.LogDebug($"Tank image file stored at: '{filePath}'");
        }

        private static string GetFilePath(DirectoryInfo dirInfo, string fileName)
        {
            var fileExtension = Path.GetExtension(fileName);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

            var fileNameWithDateTimeStamp = (fileNameWithoutExt + DateTime.Now.Date.ToString("yyyyMMdd") + "_" + DateTime.Now.TimeOfDay.ToString("hhmmss"));
            var filePath = $@"{dirInfo.FullName}/{fileNameWithDateTimeStamp}{fileExtension}";
            return filePath;
        }

        async Task<PredictionResults> PredictWaterLevel(byte[] imageBytes)
        {
            var client = new HttpClient();
            var uri = new Uri(_predictionApiBaseUrl + "/image");
            client.DefaultRequestHeaders.Add("Prediction-Key", _predictionKey);

            ByteArrayContent byteContent = new ByteArrayContent(imageBytes);

            var response = await client.PostAsync(uri, byteContent);

            var content = await response.Content.ReadAsStringAsync();
            var predictionResults = JsonConvert.DeserializeObject<PredictionResults>(content);
            return predictionResults;
        }

        private async Task<byte[]> GetImage(CancellationToken cancellationToken)
        {
            try
            {
                var client = new HttpClient();
                var uri = new Uri(_photoWebServerBaseUrl + "/capture");

                var response = await client.GetAsync(uri);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    _logger.LogError(response.ReasonPhrase);
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Capture image response: {content}");

                await Task.Delay(2000, cancellationToken);

                uri = new Uri(_photoWebServerBaseUrl + "/saved-photo");

                response = await client.GetAsync(uri);

                var contentBytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogDebug($"Saved-photo response length: {contentBytes.Length}");

                return contentBytes;
            }
            catch (Exception exception)
            {
                var message = "And error occurred calling the photo server.";
                _logger.LogError(exception, message);
                throw new Exception(message, exception);
            }
        }
    }
}
