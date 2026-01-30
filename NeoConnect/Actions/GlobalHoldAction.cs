
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

        public GlobalHoldAction(IConfiguration config, IServiceScopeFactory serviceScopeFactory)
        {
            _config = config;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public string? Name => "Global Hold";

        public string? Schedule => _config["HoldSchedule"];

        public async Task Action(CancellationToken stoppingToken)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var heatingService = scope.ServiceProvider.GetService<IHeatingService>();
                var weatherService = scope.ServiceProvider.GetService<IWeatherService>();

                var forecast = await weatherService.GetForecast(stoppingToken);
                
                await heatingService.ReduceSetTempWhenExternalTempIsWarm(forecast.ForecastDay[0], stoppingToken);
            }
        }             
    }
}
