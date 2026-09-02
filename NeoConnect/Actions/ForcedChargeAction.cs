namespace NeoConnect
{
    public class ForcedChargeAction : ScheduledAction
    {
        private const double _batteryCapacitykWh = 5.7;
        private const int _minimumSoc = 16;  // Always charge to this percentage as a minimum.
        private const double _estimatedConsumptionkWh = 12.0; // Estimated house usage, based on Average (kWh)
        private const int _startHour = 2; // super offpeak starts at 2am
        private const int _startMinute = 2; // but start at 2 minutes past
        private const int _endHour = 4; // super offpeak ends at 5am
        private const int _endMinute = 58; // but end at 2 minutes to hour

        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<ForcedChargeAction> _logger;

        public ForcedChargeAction(IConfiguration config, IServiceScopeFactory serviceScopeFactory, ILogger<ForcedChargeAction> logger, IEmailService emailService)
            : base(logger, emailService)
        {
            _config = config;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public override string Id => "forced_charge";

        public override string Name => "Forced Charge";

        public override string Description => "Forces the solar battery to charge from the grid based on the expected amount of sunlight for the day ahead.";

        public override string? Schedule => _config["ForcedChargeSchedule"];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken, bool isManualTrigger = false)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var foxEssService = scope.ServiceProvider.GetRequiredService<ISolarService>();
                var solarForecastService = scope.ServiceProvider.GetRequiredService<ISolarForecastService>();

                // Get the solar forecast
                double forecastedSolarGenerationkWh = await solarForecastService.GetSolarEstimate(stoppingToken);
                if (forecastedSolarGenerationkWh == 0.0)
                {
                    _logger.LogWarning($"Unable to retreive Solar Estimate. Will use yesterday's Force Charge settings.");
                    return;
                }

                // Calculate how much net energy the house needs beyond what solar is expected to produce                                                             
                var solarShortfallkWh = _estimatedConsumptionkWh - forecastedSolarGenerationkWh;

                // If solar covers the entire day's consumption, the deficit is zero
                if (solarShortfallkWh < 0)
                {
                    solarShortfallkWh = 0;
                }

                // Calculate target SOC percentage needed to cover solar shortfall
                int targetSoc = (int)Math.Round((solarShortfallkWh / _batteryCapacitykWh) * 100);

                // We can't use the last 10 percent of the battery, so we need to reserve it.
                targetSoc += 10;

                // Ensure target SOC is within valid range
                targetSoc = Math.Clamp(targetSoc, _minimumSoc, 100);

                _logger.LogInformation($"Target SOC at end of offpeak period: {targetSoc}%.");

                // Schedule the window on FoxESS
                await foxEssService.SetForceChargeWindow(_startHour, _startMinute, _endHour, _endMinute, targetSoc, stoppingToken);
            }
        }             
    }
}
