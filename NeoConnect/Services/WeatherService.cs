using System.Net.Http.Headers;
using System.Text.Json;

namespace NeoConnect
{
    public class WeatherService : IWeatherService
    {
        private readonly ILogger<WeatherService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _uri;
        private readonly string _apiKey;
        private readonly string _location;
        private WeatherResponse? _cachedResponse;
        private DateTime? _cacheTimestamp;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(1);
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);

        public WeatherService(ILogger<WeatherService> logger, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            _uri = config.GetValue<string>("WeatherApi:Uri") ?? throw new ArgumentNullException("Config value for WeatherApi:Uri is required");
            _apiKey = config.GetValue<string>("WeatherApi:ApiKey") ?? throw new ArgumentNullException("Config value for WeatherApi:ApiKey is required");
            _location = config.GetValue<string>("WeatherApi:Location") ?? throw new ArgumentNullException("Config value for WeatherApi:Location is required");
        }

        public async Task<WeatherResponse> GetForecast(CancellationToken stoppingToken)
        {
            await _cacheLock.WaitAsync(stoppingToken);
            try
            {
                // Check if cache is valid
                if (_cachedResponse != null && _cacheTimestamp.HasValue && DateTime.UtcNow - _cacheTimestamp.Value < _cacheExpiration)
                {
                    _logger.LogInformation("Returning cached weather forecast.");
                    return _cachedResponse;
                }

                _logger.LogInformation("Getting weather forecast.");

                using (var client = _httpClientFactory.CreateClient())
                {
                    // Add an Accept header for JSON format.
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Call the API.
                    using (HttpResponseMessage response = await client.GetAsync($"{_uri}?key={_apiKey}&q={_location}&days=1&aqi=no&alerts=no", stoppingToken))
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            _logger.LogDebug($"weather response: [{response.StatusCode}] {await response.Content.ReadAsStringAsync()}");
                        }

                        response.EnsureSuccessStatusCode();

                        // Parse the response content.
                        var result = await response.Content.ReadAsStringAsync(stoppingToken);
                        var weatherResponse = JsonSerializer.Deserialize(result, NeoConnectJsonContext.Default.WeatherResponse) ?? throw new Exception($"Error parsing weather json: {result}");

                        // Update cache
                        _cachedResponse = weatherResponse;
                        _cacheTimestamp = DateTime.UtcNow;

                        _logger.LogInformation("Weather forecast successfully retrieved.");

                        return weatherResponse;
                    }
                }
            }
            finally
            {
                _cacheLock.Release();
            }
        }
    }
}
