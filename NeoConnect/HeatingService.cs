using Microsoft.Extensions.Options;

namespace NeoConnect
{
    public class HeatingService : IHeatingService
    {
        private readonly HeatingConfig _config;
        private readonly ILogger<HeatingService> _logger;
        private readonly INeoHubService _neoHub;

        private List<string> changeList = new List<string>();
        private string lastRecipeRun;

        public HeatingService(IOptions<HeatingConfig> config, ILogger<HeatingService> logger, INeoHubService neoHub)
        {
            _config = config.Value;
            _logger = logger;
            _neoHub = neoHub;
        }

        public List<string> GetChangesMade()
        {                        
            return changeList;
        }

        public async Task Init(CancellationToken stoppingToken)
        {
            await _neoHub.Connect(stoppingToken);
        }

        public async Task Cleanup(CancellationToken stoppingToken)
        {
            await _neoHub.Disconnect(stoppingToken);
        }

        public async Task SetPreheatDurationBasedOnWeatherConditions(ForecastDay forecastToday, CancellationToken stoppingToken)
        {
            if (!_config.PreHeatOverride.Enabled)
            {
                return;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Default MaxPreheatHours: {_config.PreHeatOverride.MaxPreheatHours}c");                
            }

            var devices = (await _neoHub.GetDevices(stoppingToken)).Where(d => d.IsThermostat && !d.IsOffline);
            var profiles = await _neoHub.GetAllProfiles(stoppingToken);            

            _logger.LogInformation($"Found {devices.Count()} Devices and {profiles.Count} Profiles.");

            var engineersData = await _neoHub.GetEngineersData(stoppingToken);
            var rocByDevice = await _neoHub.GetROC(devices.Select(d => d.ZoneName).ToArray(), stoppingToken);

            foreach (var device in devices)
            {
                Profile deviceProfile;
                profiles.TryGetValue(device.ActiveProfile, out deviceProfile);

                if (deviceProfile == null)
                {
                    _logger.LogInformation($"Ignoring {device.ZoneName} as no profile set.");
                    continue;
                }

                if (deviceProfile.ProfileName == _config.SummerProfileName)
                {
                    _logger.LogInformation($"Ignoring {device.ZoneName} as Summer Profile is active.");
                    continue;
                }

                //get the next switching interval that is at least 3 hours from now (or whatever the default duration is)
                var nextInterval = _neoHub.GetNextSwitchingInterval(deviceProfile.Schedule, DateTime.Now.AddHours(_config.PreHeatOverride.MaxPreheatHours));

                //if nextInterval is null then no more intervals today, therefore do nothing
                if (nextInterval == null)
                {
                    _logger.LogInformation($"No more intervals today for {device.ZoneName}. Doing nothing.");
                    continue;
                }                
                                
                int maxPreheatDuration = 0;                

                if (nextInterval.IsWake || !_config.PreHeatOverride.OnlyEnablePreheatForWakeSchedules)
                {                    
                    //get external temperature forecast for the hours either side of the next interval.                    
                    var forecastExternalThisHour = forecastToday.Hour[nextInterval.Time.Hour];
                    var forecastExternalNextHour = forecastToday.Hour[nextInterval.Time.Hour < 23 ? nextInterval.Time.Hour + 1 : 23];

                    //we will work with the average of these two temps
                    var forecastExternalTemp = Math.Round((forecastExternalThisHour.Temp.Value + forecastExternalNextHour.Temp.Value) / 2, 2);

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug($"Forecast External Temperature at {nextInterval.Time}:00 is {forecastExternalTemp}c");
                        _logger.LogDebug($"Current Temperature of {device.ZoneName} is {device.ActualTemp}c.");
                        _logger.LogDebug($"Desired Temperature of {device.ZoneName} is {nextInterval.TargetTemp}c at {nextInterval.Time}.");
                    }

                    decimal roc = 0;
                    rocByDevice.TryGetValue(device.ZoneName, out roc);

                    //apply weightings to roc based on external temperature and aspect
                    var tempWeighting = _config.PreHeatOverride.ExternalTempROCWeightings
                        .OrderByDescending(o => o.Temp)
                        .FirstOrDefault(o => forecastExternalTemp >= o.Temp)?.Weighting ?? 1;

                    decimal sunnyAspectWeighting = 1;
                    if (forecastExternalNextHour.IsDaytime == true && forecastExternalNextHour.Condition.Code == "1000") //1000 == "Sunny"
                    {
                        // if the sun will be up and it will be sunny, apply sun-based weightings if applicable
                        sunnyAspectWeighting = _config.PreHeatOverride.SunnyAspectROCWeightings
                            .FirstOrDefault(o => o.Devices.Contains(device.ZoneName, StringComparer.OrdinalIgnoreCase))?.Weighting ?? 1;
                    }

                    var weightedRoc = roc * tempWeighting * sunnyAspectWeighting;

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug($"Rate of Change for {device.ZoneName} is {roc} minutes per degree c.");
                        _logger.LogDebug($"External Temperature Weighting: {tempWeighting}");
                        _logger.LogDebug($"Sunny Aspect Weighting: {sunnyAspectWeighting}");
                        _logger.LogDebug($"Weighted Rate of Change for {device.ZoneName} is {weightedRoc} minutes per degree c.");
                    }

                    // get the preheat duration required to achieve the desired temperature in hours
                    var temperatureIncreaseRequired = nextInterval.TargetTemp - Convert.ToDecimal(device.ActualTemp);
                    //maxPreheatDuration = (int)Math.Round((weightedRoc * temperatureIncreaseRequired) / 60, MidpointRounding.AwayFromZero);
                    maxPreheatDuration = (int)Math.Ceiling((weightedRoc * temperatureIncreaseRequired) / 60);

                    // ensure preheat duration is not longer than the max allowed
                    maxPreheatDuration = Math.Min(maxPreheatDuration, _config.PreHeatOverride.MaxPreheatHours);
                }
                
                // Set the preheat duration unless it is already set to that value
                if (maxPreheatDuration != engineersData[device.ZoneName].MaxPreheatDuration)
                {
                    await _neoHub.SetPreheatDuration(device.ZoneName, maxPreheatDuration, stoppingToken);
                    changeList.Add($"{device.ZoneName} preheat duration was changed to {maxPreheatDuration} hours.");
                }
                else
                {
                    _logger.LogInformation($"Preheat duration already set to {maxPreheatDuration} hours for {device.ZoneName}.");
                }
            }
        }
        
        public async Task RunRecipeBasedOnWeatherConditions(ForecastDay forecastToday, CancellationToken stoppingToken)
        {
            if(!_config.Recipes.Enabled)
            {                
                return;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"ExternalTempThreshold: {_config.Recipes.ExternalTempThreshold}");
                _logger.LogDebug($"Forecast External Average Temperature is {forecastToday.Day.AverageTemp}c");
            }

            string recipeToRun = _config.Recipes.WinterRecipeName;

            if (forecastToday.Day.AverageTemp >= _config.Recipes.ExternalTempThreshold)
            {
                recipeToRun = _config.Recipes.SummerRecipeName;
            }

            if (recipeToRun != _config.Recipes.LastRecipeRun)
            {
                await _neoHub.RunRecipe(recipeToRun, stoppingToken);

                // wait five seconds to allow time for recipe to complete before continuing.
                await Task.Delay(5000, stoppingToken);

                changeList.Add($"{recipeToRun} Recipe was run.");
                _config.Recipes.LastRecipeRun = recipeToRun;                
            }
            else
            {
                _logger.LogInformation($"Recipe {lastRecipeRun} has already run.");
            }
        }
    }
}
