
namespace NeoConnect
{
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
                var heatingService = scope.ServiceProvider.GetRequiredService<IHeatingService>();
                var weatherService = scope.ServiceProvider.GetRequiredService<IWeatherService>();

                var forecast = await weatherService.GetForecast(stoppingToken);

                await heatingService.Init(stoppingToken);

                await heatingService.ReduceSetTempWhenExternalTempIsWarm(forecast.ForecastDay[0], stoppingToken);

                await heatingService.Cleanup(stoppingToken);
            }
        }             
    }
}
