
namespace NeoConnect
{
    public class BathroomBoostAction : IScheduledAction
    {
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public BathroomBoostAction(IConfiguration config, IServiceScopeFactory serviceScopeFactory)
        {
            _config = config;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public string? Name => "Bathroom Boost";

        public string? Schedule => _config["BoostSchedule"];

        public async Task Action(CancellationToken stoppingToken)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var heatingService = scope.ServiceProvider.GetService<IHeatingService>();

                await heatingService.Init(stoppingToken);

                await heatingService.BoostTowelRailWhenBathroomIsCold(stoppingToken);

                await heatingService.Cleanup(stoppingToken);
            }
        }                
    }
}
