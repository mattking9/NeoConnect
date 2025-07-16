using Cronos;
using Microsoft.Extensions.Options;

namespace NeoConnect
{
    public class ActionsService
    {
        private readonly ActionsConfig _config;
        private readonly ILogger<ActionsService> _logger;        
        private readonly WeatherService _weatherService;
        private readonly NeoHubService _neoHub;        
        private readonly EmailService _emailService;

        public ActionsService(IOptions<ActionsConfig> config, ILogger<ActionsService> logger, WeatherService weatherService, NeoHubService neoHub, EmailService emailService)
        {
            _config = config.Value;
            _logger = logger;
            _weatherService = weatherService;
            _neoHub = neoHub;
            _emailService = emailService;
        }

        public async Task SetPreheatDurationBasedOnWeatherConditions(CancellationToken stoppingToken)
        {
            /*
            Problem: if its going to get warmer in the next hour (because of heat from sun), don't start heating
                
            e.g.
            at 7am
            stat says 18
            setting is 20

            so stat will call

            but outside temperature is due to reach x at 8am, naturally raising the temperature to 20+.

            this is exacerbated by preheat, which will start calling 2 hours before 7am

            Question: What outside temp causes room temp to rise organically from 18 to 20?

            Changing the differential wont work:
            if set temp is 20
            and diff is 3
            then preheat will stop heating at 7am if temp is 17 and that is no good

            We need to turn off preheat if temp is expected to be 'naturally' high
            This means that at 7am
            room will start heating if less than 19.5   (assuming 0.5 diff)
            
            BUT
            
            it is important that we don't allow the room to be uncomfortably cold at 7am just because it is due to get warm later!
            therefore need condition that room must not be lower than x

            e.g. if expected to by warm and temp not less than x below at 7am           
                
             */


            _logger.LogInformation("Preheat Override Process Starting.");

            try
            {
                var threshold = _config.PreHeatOverride.ExternalTempThresholdForCancel;
                var maxTempDifference = _config.PreHeatOverride.MaxTempDifferenceForCancel;
                var defaultDuration = _config.PreHeatOverride.DefaultPreheatDuration;

                _logger.LogDebug($"ExternalTempThresholdForCancel: {threshold}");
                _logger.LogDebug($"MaxTempDifferenceForCancel: {maxTempDifference}");
                _logger.LogDebug($"DefaultPreheatDuration: {defaultDuration}");


                var forecast = await _weatherService.GetForecast(stoppingToken);                                

                await _neoHub.Connect(stoppingToken);                                

                var liveData = await _neoHub.GetLiveData(stoppingToken);
                var profiles = await _neoHub.GetAllProfiles(stoppingToken);
                var engineersData = await _neoHub.GetEngineersData(stoppingToken);

                _logger.LogInformation($"Found {liveData.Devices.Count} Devices and {profiles.Count} Profiles.");


                foreach (var device in liveData.Devices.Where(d => d.IsThermostat && !d.IsOffline))
                {
                    var deviceProfile = profiles.FirstOrDefault(p => p.ProfileId == device.ActiveProfile);

                    var nextInterval = deviceProfile?.Schedule.GetNextSwitchingInterval();

                    //if next is null then no more intervals today so do nothing
                    if (nextInterval == null)
                    {
                        _logger.LogInformation($"No more intervals today for {device.ZoneName}. Doing nothing.");
                        continue;
                    }

                    //get outside temperature at time of next interval (round to nearest hour) from weather API
                    var forecastExternalTemp = forecast.ForecastDay[0].Hour[nextInterval.Time.Hour].Temp;


                    _logger.LogDebug($"Forecast External Temperature at {nextInterval.Time}:00 is {forecastExternalTemp}");
                    _logger.LogDebug($"Current Temperature of {device.ZoneName} is {device.ActualTemp}c.");
                    _logger.LogDebug($"Desired Temperature of {device.ZoneName} is {nextInterval.TargetTemp}c at {nextInterval.Time}.");
                    

                    var internalTempDifference = nextInterval.TargetTemp - decimal.Parse(device.ActualTemp);
                    
                    // 'Normal' Preheat is 2 hours for all stats
                    var maxPreheatDuration = defaultDuration.GetValueOrDefault(2);

                    // If forecast temp is higher than the threshold and the actual temp now is within 2 degrees of target temp at the next switching
                    // interval then cancel preheat
                    if (forecastExternalTemp >= threshold && internalTempDifference < maxTempDifference)
                    {
                        maxPreheatDuration = 0;
                    }

                    // Set the preheat duration unless it is already set to that value
                    if (maxPreheatDuration != engineersData[device.ZoneName].MaxPreheatDuration)
                    { 
                        _logger.LogInformation($"Setting preheat duration for {device.ZoneName} to {maxPreheatDuration} hours.");
                        //await _neoHub.SetPreheatDuration(device.ZoneName, maxPreheatDuration, stoppingToken);
                    }
                    else
                    {
                        _logger.LogInformation($"Preheat duration already set to {maxPreheatDuration} hours for {device.ZoneName}.");
                    }
                }

                await _emailService.SendSummaryEmail(new List<string>() { "Study was set to 2 hours", "Hall was set to 2 hours" }, stoppingToken);

                forecast = null;
                liveData = null;
                profiles = null;
                engineersData = null;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Operation was canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Execution Error. Aborting.");
            }
            finally
            {
                _logger.LogInformation("Closing NeoHub connection...");
                await _neoHub.Disconnect(stoppingToken);

                _logger.LogInformation("Process Completed.");
            }
        }
    }
}
