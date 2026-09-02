using System.Text.Json;

namespace NeoConnect
{
    public class SolarForecastService : ISolarForecastService
    {
        private readonly ILogger<SolarService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private string _host = "https://api.forecast.solar";
        private readonly double _lat = 50.592408;
        private readonly double _lon = -3.482805;
        private readonly int _tilt = 20;
        private readonly int _azimuth = 15;
        private readonly double _kwp = 16;

        public SolarForecastService(IConfiguration configuration, ILogger<SolarService> logger, IHttpClientFactory httpClientFactory)
        {
            //_lat = configuration.GetValue<string>("SolarForecast:Lat");
            //_lon = configuration.GetValue<string>("SolarForecast:Lon");
            //_tilt = configuration.GetValue<string>("SolarForecast:Tilt");
            //_azimuth = configuration.GetValue<string>("SolarForecast:Tilt");
            //_kwp = configuration.GetValue<string>("SolarForecast:kWp");
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<double> GetSolarEstimate(CancellationToken stoppingToken)
        {
            var path = $"/estimate/{_lat}/{_lon}/{_tilt}/{_azimuth}/{_kwp}";

            try
            {
                using (var client = _httpClientFactory.CreateClient())
                {


                    _logger.LogDebug($"Posting request to solar forecast api: {path}...");

                    var url = new Uri(_host + path);

                    client.DefaultRequestHeaders.Add("User-Agent", "HomeAutomationSolarCharger/1.0");

                    var response = await client.GetAsync(url, stoppingToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Request failed with status code: " + response.StatusCode);
                    }

                    var jsonString = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<SolarForecastResponse>(jsonString);

                    string todayString = DateTime.Today.ToString("yyyy-MM-dd");
                    int totalWh = 0;

                    if (data?.Result?.WattHoursDay != null &&
                        data.Result.WattHoursDay.TryGetValue(todayString, out totalWh))
                    {
                        // Convert Wh from API into kWh for your battery logic
                        return totalWh / 1000.0;
                    }

                    Console.WriteLine($"Warning: No solar data found for date {todayString}. Defaulting to 0.");
                    return 0.0;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching solar forecast: {ex.Message}");
                // Return 0 on failure to ensure your automation triggers a safe grid top-up
                return 0.0;
            }
        }
    }
}