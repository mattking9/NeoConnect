
namespace NeoConnect
{
    public class BoostAction : IScheduledAction
    {
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public BoostAction(IConfiguration config, IServiceScopeFactory serviceScopeFactory)
        {
            _config = config;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public string? Name => "Boost";

        public string? Schedule => _config["BoostSchedule"];

        public async Task Action(CancellationToken stoppingToken)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var heatingService = scope.ServiceProvider.GetRequiredService<IHeatingService>();

                await heatingService.Init(stoppingToken);

                await heatingService.BoostTowelRailWhenBathroomIsCold(stoppingToken);

                await heatingService.Cleanup(stoppingToken);
            }
        }                
    }
}
