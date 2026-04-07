
namespace NeoConnect
{
    /// <summary>
    /// Represents a scheduled action that adjusts heating settings globally based on weather forecasts.
    /// </summary>
    /// <remarks>This action retrieves the weather forecast and adjusts the heating system's settings
    /// accordingly. The schedule for this action is configured via the application settings.</remarks>
    public class GlobalHoldAction : ScheduledAction
    {
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<GlobalHoldAction> _logger;
        private readonly IEmailService _emailService;

        public GlobalHoldAction(IConfiguration config, IServiceScopeFactory serviceScopeFactory, ILogger<GlobalHoldAction> logger, IEmailService emailService)
            : base(logger, emailService)
        {
            _config = config;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _emailService = emailService;
        }

        public override string Id => "global_hold";

        public override string Name => "Global Hold";

        public override string Description => "Holds all thermostats at 0.5° below their set temperature if it is due to be warm and/or sunny in 1 hour's time.";

        public override string? Schedule => _config["HoldSchedule"];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var heatingService = scope.ServiceProvider.GetRequiredService<IHeatingService>();
                var weatherService = scope.ServiceProvider.GetRequiredService<IWeatherService>();

                var forecast = await weatherService.GetForecast(stoppingToken);

                // get the forecast conditions for the next full hour
                var forecastNextHour = forecast.ForecastDay[0].Hour[DateTime.Now.Hour < 23 ? DateTime.Now.Hour + 1 : 23];
                var forecastNextNextHour = forecast.ForecastDay[0].Hour[DateTime.Now.Hour < 22 ? DateTime.Now.Hour + 2 : 23];
                var forecastTemp = (forecastNextHour.Temp + forecastNextNextHour.Temp) / 2;
                var isSunny = forecastNextHour.IsSunny || forecastNextNextHour.IsSunny;

                // Set the temperature threshold for holding.
                // If it's forecast to be sunny in the next couple of hours, we can be more aggressive with holding as direct sun will make the house warmer.
                var threshold = isSunny ? 6.5 : 11;

                if (forecastTemp < threshold)
                {
                    _logger.LogInformation($"Skipping as forecast for next hour is {forecastTemp}c which is below threshold {threshold}c");
                    return;
                }

                await heatingService.GlobalHold(-0.5, 1, stoppingToken);

                await _emailService.SendInfoEmail("Holding all devices down 0.5c for 1 hour", stoppingToken);
            }
        }             
    }
}
