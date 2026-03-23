
namespace NeoConnect
{
    /// <summary>
    /// Represents a scheduled action that adjusts heating settings globally based on weather forecasts.
    /// </summary>
    /// <remarks>This action retrieves the weather forecast and adjusts the heating system's settings
    /// accordingly. The schedule for this action is configured via the application settings.</remarks>
    public class GlobalHoldAction : IScheduledAction
    {
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<GlobalHoldAction> _logger;
        private readonly IEmailService _emailService;

        public GlobalHoldAction(IConfiguration config, IServiceScopeFactory serviceScopeFactory, ILogger<GlobalHoldAction> logger, IEmailService emailService)
        {
            _config = config;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _emailService = emailService;
        }

        public string Id => "global_hold";

        public string Name => "Global Hold";

        public string Description => "Holds all thermostats at 0.5° below their set temperature if it is due to be warm and/or sunny in 1 hour's time.";

        public string? Schedule => _config["HoldSchedule"];

        public async Task Action(CancellationToken stoppingToken)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var heatingService = scope.ServiceProvider.GetRequiredService<IHeatingService>();
                var weatherService = scope.ServiceProvider.GetRequiredService<IWeatherService>();

                var forecast = await weatherService.GetForecast(stoppingToken);

                // get the temperature for the next hour
                var forecastNextHour = forecast.ForecastDay[0].Hour[DateTime.Now.Hour < 23 ? DateTime.Now.Hour + 1 : 23];

                var threshold = forecastNextHour.IsSunny ? 6.5 : 11;

                if (forecastNextHour.Temp < threshold)
                {
                    _logger.LogInformation($"Skipping as forecast for next hour is {forecastNextHour.Temp}c ({forecastNextHour.Condition.Text}) which is below threshold {threshold}c");
                    return;
                }

                await heatingService.GlobalHold(-0.5, 1, stoppingToken);

                await _emailService.SendInfoEmail("Holding all devices down 0.5c for 1 hour", stoppingToken);
            }
        }             
    }
}
