

namespace NeoConnect
{
    public class HeatingService : IHeatingService
    {
        private readonly ILogger<HeatingService> _logger;
        private readonly INeoHubService _neoHub;
        private readonly IEmailService _emailService;
        private readonly IReportDataService _reportDataService;

        public HeatingService(ILogger<HeatingService> logger, INeoHubService neoHub, IEmailService emailService, IReportDataService reportDataService)
        {
            _logger = logger;
            _neoHub = neoHub;
            _emailService = emailService;
            _reportDataService = reportDataService;
        }

        public async Task Init(CancellationToken stoppingToken)
        {
            await _neoHub.Connect(stoppingToken);
        }

        public async Task Cleanup(CancellationToken stoppingToken)
        {
            await _neoHub.Disconnect(stoppingToken);
        }

        /// <summary>
        /// Boosts the towel rail in the bathroom for one hour if the bathroom temperature is at least one degree below
        /// the set temperature.
        /// </summary>
        /// <param name="stoppingToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns></returns>
        public async Task BoostTowelRailWhenBathroomIsCold(CancellationToken stoppingToken)
        {
            const string BATHROOM = "Bathroom";
            const string TOWEL_RAIL = "Towel Rail";

            var devices = await _neoHub.GetDevices(stoppingToken);
            var stat = devices.FirstOrDefault(d => d.ZoneName == BATHROOM);
            var timer = devices.FirstOrDefault(d => d.ZoneName == TOWEL_RAIL);

            if (stat == null)
            {
                _logger.LogInformation($"Device named '{BATHROOM}' was not found.");
                return;
            }

            if (timer == null)
            {
                _logger.LogInformation($"Device named '{TOWEL_RAIL}' was not found.");
                return;
            }

            if (stat.IsOffline || stat.IsStandby || Convert.ToDouble(stat.SetTemp) <= 12)
            {
                _logger.LogInformation($"{BATHROOM} is in an inactive state.");
                return;
            }

            var profiles = await _neoHub.GetAllProfiles(stoppingToken);
            var bathroomProfile = profiles[stat.ActiveProfile];
            var nextComfortLevel = _neoHub.GetNextComfortLevel(bathroomProfile.Schedule, DateTime.Now);
            if (nextComfortLevel == null)
            {
                _logger.LogInformation($"{BATHROOM} has no more comfort levels today.");
                return;
            }

            //if actual temp is 1 degree less than set temp then run Boost on towel radiator for 1 hour
            var temperatureDifference = Convert.ToDouble(nextComfortLevel.TargetTemp) - Convert.ToDouble(stat.ActualTemp);
            if (Convert.ToDouble(nextComfortLevel.TargetTemp) - Convert.ToDouble(stat.ActualTemp) >= 1)
            {
                await _neoHub.Boost([timer.ZoneName], 1, stoppingToken);
                await _emailService.SendInfoEmail($"Boosted {timer.ZoneName}", stoppingToken);
            }
            else
            {
                _logger.LogInformation($"Bathroom Boost not required this time. Bathroom is currently {(temperatureDifference < 0 ? 1-temperatureDifference : temperatureDifference) }c {(temperatureDifference < 0 ? "above" : "below")} target.");
            }
        }


        /// <summary>
        /// If it is X degress or more when this method runs, it will turn all stats down by half a degree for 1 hour because the sun will provide additional warming during this time.
        /// </summary>
        /// <param name="forecastToday"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public async Task ReduceSetTempWhenExternalTempIsWarm(ForecastDay forecastToday, CancellationToken stoppingToken)
        {            
            // get the temperature for the next hour
            var forecastNextHour = forecastToday.Hour[DateTime.Now.Hour < 23 ? DateTime.Now.Hour + 1 : 23];

            var threshold = forecastNextHour.IsSunny ? 7 : 12;

            if (forecastNextHour.Temp < threshold)
            {
                _logger.LogInformation($"Skipping as external temperature for next hour is expected to be {forecastNextHour.Temp}c which is below threshold {threshold}c");
                return;
            }

            // fetch all the necessary data from the NeoHub
            var devices = (await _neoHub.GetDevices(stoppingToken)).Where(d => d.IsThermostat && !d.IsOffline && d.ActiveProfile != 0 && !d.IsStandby);

            var holdGroup = "ReduceWhenWarm";
            foreach (var device in devices)
            {                
                await _neoHub.Hold(holdGroup, [device.ZoneName], Convert.ToDouble(device.SetTemp) - 0.5, 1, stoppingToken);
            }
            await _emailService.SendInfoEmail(devices.Select(d => $"Holding {d.ZoneName} down 0.5c for 1 hour"), stoppingToken);
        }

        public async Task LogDeviceStatuses(CancellationToken stoppingToken)
        {
            var devices = (await _neoHub.GetDevices(stoppingToken)).Where(d => !d.IsOffline && d.ActiveProfile != 0 && !d.IsStandby);            

            _logger.LogInformation($"Writing device statuses to database.");
            _reportDataService.AddDeviceData(devices, 0);
        }
    }
}
