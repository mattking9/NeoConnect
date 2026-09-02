namespace NeoConnect
{
    /// <summary>
    /// Represents a scheduled action that adjusts heating settings globally based on weather forecasts.
    /// </summary>
    /// <remarks>This action retrieves the weather forecast and adjusts the heating system's settings
    /// accordingly. The schedule for this action is configured via the application settings.</remarks>
    public class ForcedChargeAction : ScheduledAction
    {
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
                var solarService = scope.ServiceProvider.GetRequiredService<ISolarService>();
                var solarForecastService = scope.ServiceProvider.GetRequiredService<ISolarForecastService>();

                // Get the solar forecast
                double expectedSolarKwh = await solarForecastService.GetSolarEstimate(stoppingToken);                

                // Get the current battery state of charge (SoC) and capacity from FoxESS API
                var solarData = await solarService.GetRealtimeData(stoppingToken);
                double currentSoC = Convert.ToDouble(solarData.SoC); // Current battery state of charge in percentage

                double batteryCapacitykWh = 5.7;
                int minimumSoc = 20;  // Always charge to this percentage as a minimum                
                double expectedLoadkWh = 12.0; // Estimated house usage, based on Average (kWh)

                // Calculate Requirement
                double minimumBatterykWh = batteryCapacitykWh * (minimumSoc / 100.0);
                double currentBatterykWh = batteryCapacitykWh * (currentSoC / 100.0);
                double netEnergy = currentBatterykWh + expectedSolarKwh - expectedLoadkWh;

                double gridChargeNeededKwh = 0.0;

                if (netEnergy < minimumBatterykWh)
                {
                    gridChargeNeededKwh = minimumBatterykWh - netEnergy;

                    // Cap it at maximum physical headroom
                    double maxPhysicalChargePossible = batteryCapacitykWh - currentBatterykWh;
                    gridChargeNeededKwh = Math.Min(gridChargeNeededKwh, maxPhysicalChargePossible);
                }

                // Schedule the window on FoxESS                
                double chargeRateKw = 4; // This is determined in the Fox ESS App
                double hoursRequired = gridChargeNeededKwh / chargeRateKw;

                Console.WriteLine($"Action Required: Charge {gridChargeNeededKwh:F2} kWh from grid.");
                Console.WriteLine($"Charging for {hoursRequired:F2} hours.");

                int startHour = 2; // super offpeak starts at 2am
                int startMinute = 10; // but start at 10 past hour
                int maxEndHour = 4; // super offpeak ends at 5am
                int maxEndMinute = 50; // but end at 10 to hour

                TimeSpan startTime = new TimeSpan(startHour, startMinute, 0);
                TimeSpan endTime = startTime.Add(TimeSpan.FromHours(hoursRequired));
                TimeSpan maxEndTime = new TimeSpan(maxEndHour, maxEndMinute, 0);

                // Ensure that end time cannot exceed off peak window
                if (endTime > maxEndTime)
                {
                    endTime = maxEndTime;
                }

                await solarService.SetForceChargeWindow(endTime != startTime ? 1 : 0, startHour, startMinute, endTime.Hours, endTime.Minutes, stoppingToken);               
            }
        }             
    }
}
