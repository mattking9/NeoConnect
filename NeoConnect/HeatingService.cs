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

        public async Task SetMaxPreheatDurationBasedOnWeatherConditions(ForecastDay forecastToday, CancellationToken stoppingToken)
        {
            if (!_config.PreHeatOverride.Enabled)
            {
                return;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Default MaxPreheatHours: {_config.PreHeatOverride.MaxPreheatHours}c");                
            }

            //fetch all the necessary data from the NeoHub
            var devices = (await _neoHub.GetDevices(stoppingToken)).Where(d => d.IsThermostat && !d.IsOffline && d.ActiveProfile != 0);
            var profiles = await _neoHub.GetAllProfiles(stoppingToken);            
            var engineersData = await _neoHub.GetEngineersData(stoppingToken);
            var rocData = await _neoHub.GetROCData(devices.Select(d => d.ZoneName).ToArray(), stoppingToken);

            _logger.LogInformation($"Found {devices.Count()} Devices and {profiles.Count} Profiles.");

            foreach (var device in devices)
            {
                Profile deviceProfile;
                profiles.TryGetValue(device.ActiveProfile, out deviceProfile);

                // Skip device if is in Standby mode or if set temperature is at or below frost temperature
                if (device.IsStandby || Convert.ToDouble(device.SetTemp) <= engineersData[device.ZoneName].FrostTemp)
                {
                    _logger.LogInformation($"Ignoring {device.ZoneName}. Standby Mode or Anti-Frost Setting.");
                    continue;
                }

                // Get the next switching interval that is at least 3 hours from now (or whatever the default duration is)
                var nextInterval = _neoHub.GetNextSwitchingInterval(deviceProfile.Schedule, DateTime.Now.AddHours(_config.PreHeatOverride.MaxPreheatHours));

                // If nextInterval is null then no more intervals today, therefore do nothing
                if (nextInterval == null)
                {
                    _logger.LogInformation($"Ignoring {device.ZoneName}. No more intervals today.");
                    continue;
                }

                // Get the rate of change for this device
                int roc = 0;
                rocData.TryGetValue(device.ZoneName, out roc);

                // Calculate the max preheat duration required
                int maxPreheatDuration = CalculateMaxPreheatDuration(forecastToday, roc, device, nextInterval);                

                // Apply the preheat duration (unless it is already set to that value)
                if (maxPreheatDuration != engineersData[device.ZoneName].MaxPreheatDuration)
                {
                    await _neoHub.SetPreheatDuration(device.ZoneName, maxPreheatDuration, stoppingToken);
                    changeList.Add($"Max Preheat duration was changed to {maxPreheatDuration} hours for {device.ZoneName}.");
                }
                else
                {
                    _logger.LogInformation($"Max Preheat duration already set to {maxPreheatDuration} hours for {device.ZoneName}.");
                }
            }
        }

        private int CalculateMaxPreheatDuration(ForecastDay forecastToday, int roc, NeoDevice device, ComfortLevel nextInterval)
        {
            //get external temperature forecast for the hours either side of the next interval.                    
            var forecastHourOf = forecastToday.Hour[nextInterval.Time.Hour];
            var forecastHourAfter = forecastToday.Hour[nextInterval.Time.Hour < 23 ? nextInterval.Time.Hour + 1 : 23];
            var forecastExternalTemp = AverageTemp(forecastHourOf, forecastHourAfter);

            //apply weightings to roc based on external temperature and aspect
            var tempWeighting = GetExternalTempWeighting(forecastExternalTemp);
            var sunnyAspectWeighting = GetSunnyAspectWeighting(device, forecastHourAfter);

            var weightedRoc = roc * tempWeighting * sunnyAspectWeighting;

            // get the expected preheat duration required to achieve the desired temperature in hours
            var temperatureIncreaseRequired = Math.Max(0, nextInterval.TargetTemp - Convert.ToDouble(device.ActualTemp));
            var maxPreheatDuration = (int)Math.Ceiling((weightedRoc * temperatureIncreaseRequired) / 60);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"###################################################");
                _logger.LogDebug($"# Device: {device.ZoneName}");
                _logger.LogDebug($"# Current Temperature: {device.ActualTemp}c.");
                _logger.LogDebug($"# Desired Temperature: {nextInterval.TargetTemp}c (at {nextInterval.Time}).");
                _logger.LogDebug($"# Rate of Change: {roc} minutes per degree c.");
                _logger.LogDebug($"# External Temperature Weighting (for {forecastExternalTemp}c): {tempWeighting}");
                _logger.LogDebug($"# Sunny Aspect Weighting: {sunnyAspectWeighting}");
                _logger.LogDebug($"# Weighted Rate of Change: {weightedRoc} minutes per degree c.");
                _logger.LogDebug($"# Calculated Max Preheat Required: {maxPreheatDuration}h.");
                _logger.LogDebug($"###################################################");
            }

            // ensure preheat duration is not longer than the max allowed
            maxPreheatDuration = Math.Min(maxPreheatDuration, _config.PreHeatOverride.MaxPreheatHours);
            return maxPreheatDuration;
        }

        private double GetSunnyAspectWeighting(NeoDevice device, ForecastHour forecast)
        {
            var sunnyAspectWeighting = 1.0;
            if (forecast.IsDaytime == 1 && forecast.Condition?.Code == 1000) //1000 == "Sunny"
            {
                // if the sun will be up and weather condition will be sunny, apply sun-based weighting
                sunnyAspectWeighting = _config.PreHeatOverride.SunnyAspectROCWeightings
                    .FirstOrDefault(o => o.Devices.Contains(device.ZoneName, StringComparer.OrdinalIgnoreCase))?.Weighting ?? 1;
            }

            return sunnyAspectWeighting;
        }

        private double GetExternalTempWeighting(double forecastExternalTemp)
        {
            return _config.PreHeatOverride.ExternalTempROCWeightings
                                    .OrderByDescending(o => o.Temp)
                                    .FirstOrDefault(o => forecastExternalTemp >= o.Temp)?.Weighting ?? 1;
        }

        private static double AverageTemp(params ForecastHour[] forecastHours)
        {
            return Math.Round(forecastHours.Select(fh => fh.Temp).Average(), 1);
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
