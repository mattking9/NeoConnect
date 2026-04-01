
namespace NeoConnect
{
    /// <summary>
    /// Represents a scheduled action that boosts the towel rail heating in the bathroom when the temperature is cold.
    /// </summary>
    /// <remarks>This action is triggered based on a configurable schedule and ensures that the bathroom towel
    /// rail is heated when the bathroom temperature falls below a certain threshold. The schedule is defined in the
    /// application configuration under the key <c>BoostSchedule</c>.</remarks>
    public class BathroomBoostAction : ScheduledAction
    {
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public BathroomBoostAction(IConfiguration config, IServiceScopeFactory serviceScopeFactory, IEmailService emailService, ILogger<BathroomBoostAction> logger) : base(logger, emailService)
        {
            _config = config;
            _serviceScopeFactory = serviceScopeFactory;            
        }

        public override string Id => "bathroom_boost";

        public override string Name => "Bathroom Boost";

        public override string Description => "Turns on the Bathroom Towel Radiator (in addition to UFH) if the Bathroom is 1° or more below it's target temperature.";

        public override string? Schedule => _config["BoostSchedule"];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var heatingService = scope.ServiceProvider.GetRequiredService<IHeatingService>();
                await heatingService.BoostTowelRailWhenBathroomIsCold(stoppingToken);
            }
        }                
    }
}
