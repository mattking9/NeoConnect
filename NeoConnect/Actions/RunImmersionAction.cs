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
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<RunImmersionAction> _logger;

        private DateTime lastReachedTemperatureAt;

        private const decimal FeedInThreshold = 3.2M;
        private const decimal GenerationThreshold = 3.2M;
        private const decimal BatterySoCThreshold = 90;

        public RunImmersionAction(IConfiguration config, IEmailService emailService, IServiceScopeFactory serviceScopeFactory, ILogger<RunImmersionAction> logger)
        {
            _config = config;
            _emailService = emailService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public string? Name => "Run Immersion";

        public string? Schedule => _config["RunImmersionSchedule"];

        public async Task Action(CancellationToken stoppingToken)
        {            
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var solarService = scope.ServiceProvider.GetRequiredService<ISolarService>();
                var immersionService = scope.ServiceProvider.GetRequiredService<IImmersionService>();

                // Exit if immersion reached temperature within the last 3 hours
                if (DateTime.Now < lastReachedTemperatureAt.AddHours(3))
                {
                    _logger.LogInformation("Exiting as Immersion reached temperature within the last 3 hours");
                    return;
                }

                // Test level of exported power for 5 minutes to see if it remains above threshold
                _logger.LogInformation("Testing exported power for 5 minutes");
                var isStableExport = false;
                for (int i = 0; i < 6; i++)
                {
                    var solarData = await solarService.GetRealtimeData();
                    isStableExport = (solarData.FeedInPower >= FeedInThreshold && solarData.SoC >= BatterySoCThreshold);
                    if (!isStableExport)
                    {
                        _logger.LogInformation($"Exiting as Solar Export is below {FeedInThreshold}kW or Battery Charge is below {BatterySoCThreshold}%");
                        break;
                    }
                    if (i < 6)
                    {
                        await Task.Delay(60 * 1000);
                    }
                }
                
                if (isStableExport)
                {
                    _logger.LogInformation("Solar Export thresholds met"); 
                    var success = await immersionService.TurnOnDevice();

                    if (success)
                    {
                        await _emailService.SendInfoEmail("Immersion was turned ON.", stoppingToken);

                        _logger.LogInformation("Starting Solar Output Monitoring loop");

                        var startedAt = DateTime.Now;
                        var loop = true;                        

                        while (loop && !stoppingToken.IsCancellationRequested)
                        {
                            // wait 3 minutes before executing
                            await Task.Delay(3 * 60 * 1000);

                            _logger.LogInformation("Monitoring Solar Output");
                                                        
                            var solarData = await solarService.GetRealtimeData();

                            // we assume immersion has reached target temperature if load is less than the power it draws
                            var isHeatingComplete = solarData.Load < 2.8M;                            

                            // Turn immersion OFF if any of the following are true:
                            // - Immersion is up to temperature
                            // - Solar is generating less than 3.2kW
                            // - Battery charge is below 90%
                            // - Job has been running for 2 hours (failsafe)
                            if (isHeatingComplete || solarData.GeneratedPower <= GenerationThreshold || solarData.SoC <= BatterySoCThreshold || DateTime.Now >= startedAt.AddHours(2))
                            {
                                success = await immersionService.TurnOffDevice();

                                if (success)
                                {
                                    loop = false;

                                    string message;

                                    if (isHeatingComplete)
                                    {                                                                                
                                        if (DateTime.Now > lastReachedTemperatureAt.AddHours(12))
                                        {
                                            // Turn off Gas-heated Hot Water for 12 hours
                                            var heatingService = scope.ServiceProvider.GetRequiredService<IHeatingService>();
                                            await heatingService.TurnOffHotWater(12, stoppingToken);
                                            message = "Immersion was turned OFF after reaching target temperature. Gas Hot Water was also turned OFF for 12 hours";
                                        }
                                        else
                                        {
                                            message = "Immersion was turned OFF after reaching target temperature.";
                                        }

                                        lastReachedTemperatureAt = DateTime.Now;
                                    }
                                    else
                                    {
                                        message = "Immersion was turned OFF before reaching target temperature.";
                                    }

                                    _logger.LogInformation(message);
                                    await _emailService.SendInfoEmail(message, stoppingToken);
                                }
                            }                            
                        }
                    }
                }
            }
        }        
    }
}
