using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace NeoConnect
{
    public class WeatherService
    {
        private readonly ILogger<WeatherService> _logger;
        private readonly IConfiguration _config;
        private readonly string _uri;
        private readonly string _apiKey;
        private readonly string _location;

        public WeatherService(ILogger<WeatherService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;

            _uri = _config.GetValue<string>("WeatherApi:Uri") ?? throw new ArgumentNullException("Config value for WeatherApi:Uri is required");
            _apiKey = _config.GetValue<string>("WeatherApi:ApiKey") ?? throw new ArgumentNullException("Config value for WeatherApi:ApiKey is required");
            _location = _config.GetValue<string>("WeatherApi:Location") ?? throw new ArgumentNullException("Config value for WeatherApi:Location is required");
        }

        public async Task<Forecast> GetForecast(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Getting weather forecast...");

            using (HttpClient client = new HttpClient())
            {
                // Add an Accept header for JSON format.
                client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

                // List data response.
                HttpResponseMessage response = await client.GetAsync($"{_uri}?key={_apiKey}&q={_location}&days=1&aqi=no&alerts=no", stoppingToken);

                _logger.LogDebug($"weather response: [{response.StatusCode}] {await response.Content.ReadAsStringAsync()}");

                response.EnsureSuccessStatusCode();

                // Parse the response content.
                var result = await response.Content.ReadFromJsonAsync<WeatherResponse>(stoppingToken) ?? throw new Exception($"Error parsing weather response json.");

                _logger.LogInformation("Weather forecast successfully retrieved.");

                return result.Forecast;
            }
        }
    }
}
