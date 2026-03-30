using System.Diagnostics;
using System.Text.Json;
using System.Timers;
using static System.Collections.Specialized.BitVector32;

namespace NeoConnect
{
    /// <summary>
    /// Represents a scheduled action that collects device status data for reporting purposes.
    /// </summary>
    /// <remarks>This action is designed to be executed on a schedule defined in the application
    /// configuration. It initializes the heating service, reports device statuses, and performs cleanup
    /// operations.</remarks>
    public class RunImmersionAction : IScheduledAction
    {
        private const decimal FeedInThreshold = 3.2M;
        private const decimal BatterySoCThreshold = 90;

        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<RunImmersionAction> _logger;

        public RunImmersionAction(IConfiguration config, IEmailService emailService, IServiceScopeFactory serviceScopeFactory, ILogger<RunImmersionAction> logger)
        {
            _config = config;
            _emailService = emailService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public string Id => "run_immersion";

        public string Name => "Run Immersion";

        public string Description => "Turns on the immersion when solar is exporting more than 3kW, then polls to ensure solar continues to generate enough.";

        public string? Schedule => _config["RunImmersionSchedule"];

        public async Task Action(CancellationToken stoppingToken)
        {            
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var solarService = scope.ServiceProvider.GetRequiredService<ISolarService>();
                var immersionService = scope.ServiceProvider.GetRequiredService<IImmersionService>();

                SolarData solarData = null;

                // Get 3 readings to see if level of exported power remains consistently above threshold
                _logger.LogInformation("Testing exported power.");
                var isStableExport = false;                
                for (int i = 0; i < 3; i++)
                {
                    solarData = await solarService.GetRealtimeData(stoppingToken);

                    if (solarData.TimeUntilNextUpdate < TimeSpan.Zero)
                    {
                        _logger.LogWarning("Failed to retrieve up-to-date solar data");
                        break;
                    }

                    isStableExport = (solarData.FeedInPower >= FeedInThreshold && solarData.SoC >= BatterySoCThreshold);
                    if (!isStableExport)
                    {
                        _logger.LogInformation($"Solar Export is below {FeedInThreshold}kW or Battery Charge is below {BatterySoCThreshold}%");
                        break;
                    }
                    if (i < 2)
                    {
                        // Wait until next update to solar data (expected to be 5 minutes from last update)
                        await Task.Delay(solarData.TimeUntilNextUpdate, stoppingToken);
                    }
                }
                
                if (isStableExport)
                {
                    _logger.LogInformation("Solar Export thresholds met"); 
                    
                    await immersionService.TurnOnDevice(stoppingToken);

                    var isOn = true;

                    try
                    {
                        await _emailService.SendInfoEmail("Immersion was turned ON.", stoppingToken);

                        _logger.LogInformation("Starting Solar Output Monitoring loop");

                        var startedAt = DateTime.Now;                        

                        while (isOn && !stoppingToken.IsCancellationRequested)
                        {
                            // Wait until next update to solar data (expected to be 5 minutes from last update)
                            await Task.Delay(solarData.TimeUntilNextUpdate, stoppingToken);

                            _logger.LogInformation("Checking Solar Output");

                            solarData = await solarService.GetRealtimeData(stoppingToken);
                            
                            // If data is older than expected then we can't trust it.
                            var isStaleData = solarData.TimeUntilNextUpdate < TimeSpan.Zero;                                                        

                            // we assume immersion has reached target temperature if load is less than the power it draws
                            var isHeatingComplete = solarData.Load < 2.8M;

                            // Turn immersion OFF if any of the following are true:
                            // - Data is stale (cannot trust the readings to make a decision)
                            // - Immersion is up to temperature
                            // - Solar is generating less than is being consumed
                            // - Battery charge is below 90%
                            // - Job has been running for 2 hours (failsafe)
                            if (isStaleData || isHeatingComplete || solarData.GeneratedPower < solarData.Load || solarData.SoC <= BatterySoCThreshold || DateTime.Now >= startedAt.AddHours(2))
                            {
                                await immersionService.TurnOffDevice(stoppingToken);

                                isOn = false;

                                if (isHeatingComplete)
                                {
                                    // Turn off Boiler-heated Hot Water for 8 hours
                                    var heatingService = scope.ServiceProvider.GetRequiredService<IHeatingService>();
                                    await heatingService.TurnOffHotWater(8, stoppingToken);

                                    await _emailService.SendInfoEmail([
                                        "Immersion was turned OFF (target temperature was reached)",
                                        "Boiler Hot Water was turned OFF for 8 hours"
                                    ], stoppingToken);

                                    _logger.LogInformation("Pausing Action for 2 hours");
                                    await Task.Delay(2 * 60 * 60 * 1000, stoppingToken); // 2 hours
                                }
                                else
                                {
                                    await _emailService.SendInfoEmail("Immersion was turned OFF (process was aborted before target temperature was reached)", stoppingToken);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Ensure immersion is turned off if it is on when something goes wrong.
                        if (isOn)
                        {                            
                            await immersionService.TurnOffDevice(stoppingToken);
                            _logger.LogWarning("Immersion was turned OFF after exception was thrown.");
                        }
                        throw;
                    }
                }
            }
        }
    }
}
