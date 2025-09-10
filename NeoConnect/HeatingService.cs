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
            var threshold = _config.PreHeatOverride.ExternalTempThresholdForCancel;
            var maxTempDifference = _config.PreHeatOverride.MaxTempDifferenceForCancel;
            var defaultDuration = _config.PreHeatOverride.DefaultPreheatDuration ?? 2;

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"ExternalTempThresholdForCancel: {threshold}c");
                _logger.LogDebug($"MaxTempDifferenceForCancel: {maxTempDifference}c");
                _logger.LogDebug($"DefaultPreheatDuration: {defaultDuration}h");
            }

            var devices = await _neoHub.GetDevices(stoppingToken);
            var profiles = await _neoHub.GetAllProfiles(stoppingToken);
            var engineersData = await _neoHub.GetEngineersData(stoppingToken);

            _logger.LogInformation($"Found {devices.Count} Devices and {profiles.Count} Profiles.");

            foreach (var device in devices.Where(d => d.IsThermostat && !d.IsOffline))
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

                //get the next switching interval that is at least 2 hours from now (or whatever the default duration is)
                var nextInterval = _neoHub.GetNextSwitchingInterval(deviceProfile.Schedule, DateTime.Now.AddHours(defaultDuration));

                //if nextInterval is null then no more intervals today, therefore do nothing
                if (nextInterval == null)
                {
                    _logger.LogInformation($"No more intervals today for {device.ZoneName}. Doing nothing.");
                    continue;
                }

                //get outside temperature at time of next interval from weather API
                var forecastExternalTemp = forecastToday.Hour[nextInterval.Time.Hour].Temp;

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug($"Forecast External Temperature at {nextInterval.Time}:00 is {forecastExternalTemp}c");
                    _logger.LogDebug($"Current Temperature of {device.ZoneName} is {device.ActualTemp}c.");
                    _logger.LogDebug($"Desired Temperature of {device.ZoneName} is {nextInterval.TargetTemp}c at {nextInterval.Time}.");
                }

                var internalTempDifference = nextInterval.TargetTemp - decimal.Parse(device.ActualTemp);

                // 'Normal' Preheat is 2 hours for all stats
                var maxPreheatDuration = defaultDuration;

                // If forecast temp is higher than the threshold and the actual temp now is within 2 degrees of target temp at the next switching
                // interval then cancel preheat
                if (forecastExternalTemp >= threshold && internalTempDifference < maxTempDifference)
                {
                    maxPreheatDuration = 0;
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
