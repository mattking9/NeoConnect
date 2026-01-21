
namespace NeoConnect
{
    /// <summary>
    /// Represents a scheduled action that boosts the towel rail heating in the bathroom when the temperature is cold.
    /// </summary>
    /// <remarks>This action is triggered based on a configurable schedule and ensures that the bathroom towel
    /// rail is heated when the bathroom temperature falls below a certain threshold. The schedule is defined in the
    /// application configuration under the key <c>BoostSchedule</c>.</remarks>
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
