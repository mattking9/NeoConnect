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
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<RunImmersionAction> _logger;

        private const decimal FeedInThreshold = 3.2M;
        private const decimal BatterySoCThreshold = 90;

        public RunImmersionAction(IConfiguration config, IEmailService emailService, IServiceScopeFactory serviceScopeFactory, ILogger<RunImmersionAction> logger)
        {
            _config = config;
            _emailService = emailService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public string Id => "run_immersion";

        public string Name => "Run Immersion";

        public string Description => "Turns on the immersion when solar is exporting more than 3kW, then polls the ensure that solar is still generating more than immersion is consuming.";

        public string? Schedule => _config["RunImmersionSchedule"];

        public async Task Action(CancellationToken stoppingToken)
        {            
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var solarService = scope.ServiceProvider.GetRequiredService<ISolarService>();
                var immersionService = scope.ServiceProvider.GetRequiredService<IImmersionService>();

                // Test level of exported power for 5 minutes to see if it remains above threshold
                _logger.LogInformation("Testing exported power for 5 minutes");
                var isStableExport = false;
                for (int i = 0; i < 5; i++)
                {
                    var solarData = await solarService.GetRealtimeData();
                    isStableExport = (solarData.FeedInPower >= FeedInThreshold && solarData.SoC >= BatterySoCThreshold);
                    if (!isStableExport)
                    {
                        _logger.LogInformation($"Solar Export is below {FeedInThreshold}kW or Battery Charge is below {BatterySoCThreshold}%");
                        break;
                    }
                    if (i < 5)
                    {
                        await Task.Delay(60 * 1000);
                    }
                }
                
                if (isStableExport)
                {
                    _logger.LogInformation("Solar Export thresholds met"); 
                    
                    await immersionService.TurnOnDevice();
                                        
                    await _emailService.SendInfoEmail("Immersion was turned ON.", stoppingToken);

                    _logger.LogInformation("Starting Solar Output Monitoring loop");

                    var startedAt = DateTime.Now;
                    var isOn = true;                        

                    while (isOn)
                    {
                        // wait 3 minutes before executing
                        await Task.Delay(3 * 60 * 1000);

                        _logger.LogInformation("Monitoring Solar Output");
                                                        
                        var solarData = await solarService.GetRealtimeData();

                        // we assume immersion has reached target temperature if load is less than the power it draws
                        var isHeatingComplete = solarData.Load < 2.8M;

                        // Turn immersion OFF if any of the conditions are met
                        if ( 
                            isHeatingComplete || 
                            solarData.GeneratedPower < solarData.Load || 
                            solarData.SoC <= BatterySoCThreshold || 
                            DateTime.Now >= startedAt.AddHours(2) || 
                            stoppingToken.IsCancellationRequested)
                        {
                            await immersionService.TurnOffDevice();
                                
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
                                await Task.Delay(2 * 60 * 60 * 1000); // 2 hours
                            }
                            else
                            {
                                await _emailService.SendInfoEmail("Immersion was turned OFF (process was aborted before target temperature was reached)", stoppingToken);
                            }                                
                        }                            
                    }
                }
            }
        }        
    }
}
