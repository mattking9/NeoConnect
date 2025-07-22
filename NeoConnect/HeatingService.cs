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
            changeList.Clear();

            await _neoHub.Disconnect(stoppingToken);
        }

        /// <summary>
        /// Problem: if its going to get warmer in the next hour (because of heat from sun), don't start heating
        /// 
        /// e.g.
        /// at 7am
        /// stat says 18
        /// setting is 20
        /// 
        /// so stat will call
        /// 
        /// but outside temperature is due to reach x at 8am, naturally raising the temperature to 20+.
        /// 
        ///     this is exacerbated by preheat, which will start calling 2 hours before 7am
        /// 
        /// Question: What outside temp causes room temp to rise organically from 18 to 20?
        /// 
        /// Changing the differential wont work:
        ///             if set temp is 20
        /// and diff is 3
        /// then preheat will stop heating at 7am if temp is 17 and that is no good
        /// 
        /// We need to turn off preheat if temp is expected to be 'naturally' high
        /// This means that at 7am
        /// room will start heating if less than 19.5   (assuming 0.5 diff)
        /// 
        /// BUT
        /// 
        /// 
        /// it is important that we don't allow the room to be uncomfortably cold at 7am just because it is due to get warm later!
        /// therefore need condition that room must not be lower than x
        /// 
        /// e.g. if expected to by warm and temp not less than x below at 7am
        /// </summary>
        /// <param name="forecastToday"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public async Task SetPreheatDurationBasedOnWeatherConditions(ForecastDay forecastToday, CancellationToken stoppingToken)
        {
            var threshold = _config.PreHeatOverride.ExternalTempThresholdForCancel;
            var maxTempDifference = _config.PreHeatOverride.MaxTempDifferenceForCancel;
            var defaultDuration = _config.PreHeatOverride.DefaultPreheatDuration;

            _logger.LogDebug($"ExternalTempThresholdForCancel: {threshold}");
            _logger.LogDebug($"MaxTempDifferenceForCancel: {maxTempDifference}");
            _logger.LogDebug($"DefaultPreheatDuration: {defaultDuration}");

            var devices = await _neoHub.GetDevices(stoppingToken);
            var profiles = await _neoHub.GetAllProfiles(stoppingToken);
            var engineersData = await _neoHub.GetEngineersData(stoppingToken);

            _logger.LogInformation($"Found {devices.Count} Devices and {profiles.Count} Profiles.");

            foreach (var device in devices.Where(d => d.IsThermostat && !d.IsOffline))
            {
                var deviceProfile = profiles.FirstOrDefault(p => p.ProfileId == device.ActiveProfile);

                if (deviceProfile != null && deviceProfile.ProfileName == _config.SummerProfileName)
                {
                    _logger.LogInformation($"Skipping preheat override for {device.ZoneName} as it is set to Summer Mode.");
                    continue;
                }

                var nextInterval = deviceProfile?.Schedule.GetNextSwitchingInterval();

                //if nextInterval is null then no more intervals today, therefore do nothing
                if (nextInterval == null)
                {
                    _logger.LogInformation($"No more intervals today for {device.ZoneName}. Doing nothing.");
                    continue;
                }

                //get outside temperature at time of next interval from weather API
                var forecastExternalTemp = forecastToday.Hour[nextInterval.Time.Hour].Temp;

                _logger.LogDebug($"Forecast External Temperature at {nextInterval.Time}:00 is {forecastExternalTemp}c");
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
                    await _neoHub.SetPreheatDuration(device.ZoneName, maxPreheatDuration, stoppingToken);
                    changeList.Add($"{device.ZoneName} preheat duration was changed to {maxPreheatDuration} hours.");
                }
                else
                {
                    _logger.LogInformation($"Preheat duration already set to {maxPreheatDuration} hours for {device.ZoneName}.");
                }
            }

            devices.Clear();
            profiles.Clear();
            engineersData.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="forecastToday"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public async Task RunRecipeBasedOnWeatherConditions(ForecastDay forecastToday, CancellationToken stoppingToken)
        {
            _logger.LogDebug($"ExternalTempThreshold: {_config.Recipes.ExternalTempThreshold}");
            _logger.LogDebug($"Forecast External Average Temperature is {forecastToday.Day.AverageTemp}c");

            string recipeToRun = _config.Recipes.WinterRecipeName;

            if (forecastToday.Day.AverageTemp >= _config.Recipes.ExternalTempThreshold)
            {
                recipeToRun = _config.Recipes.SummerRecipeName;
            }

            if (recipeToRun != lastRecipeRun)
            {
                await _neoHub.RunRecipe(recipeToRun, stoppingToken);
                changeList.Add($"{recipeToRun} Recipe was run.");
                lastRecipeRun = recipeToRun;
            }
            else
            {
                _logger.LogInformation($"Recipe {lastRecipeRun} has already run.");
            }
        }
    }
}
