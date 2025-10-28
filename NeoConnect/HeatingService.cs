
namespace NeoConnect
{
    public class HeatingService : IHeatingService
    {
        private readonly ILogger<HeatingService> _logger;
        private readonly INeoHubService _neoHub;

        public HeatingService(ILogger<HeatingService> logger, INeoHubService neoHub)
        {
            _logger = logger;
            _neoHub = neoHub;
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
            _logger.LogInformation("** BoostTowelRailWhenBathroomIsCold **");

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

            //if actual temp is 1 degree less than set temp then run Boost on towel radiator for 1 hour
            if (Convert.ToDouble(stat.SetTemp) - Convert.ToDouble(stat.ActualTemp) >= 1)
            {
                await _neoHub.Boost([timer.ZoneName], 1, stoppingToken);
            }
            else
            {
                _logger.LogInformation($"Bathroom Boost not required this time.");
            }
        }


        /// <summary>
        /// If it is 11c or more when this method runs, it will turn all stats down by half a degree for 1 hour because the sun will provide additional warming during this time.
        /// </summary>
        /// <param name="forecastToday"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public async Task ReduceSetTempWhenExternalTempIsWarm(ForecastDay forecastToday, CancellationToken stoppingToken)
        {
            _logger.LogInformation("** ReduceSetTempWhenExternalTempIsWarm **");

            // get the temperature for the next hour
            var forecastNextHour = forecastToday.Hour[DateTime.Now.Hour < 23 ? DateTime.Now.Hour + 1 : 23];
            if (forecastNextHour.Temp < 11)
            {
                _logger.LogInformation("Skipping as external temperature for next hour is expected to be below 11c");
            }

            // fetch all the necessary data from the NeoHub
            var devices = (await _neoHub.GetDevices(stoppingToken)).Where(d => d.IsThermostat && !d.IsOffline && d.ActiveProfile != 0 && !d.IsStandby);                               

            _logger.LogInformation($"Found {devices.Count()} Devices.");

            foreach (var device in devices)
            {                
                await _neoHub.Hold(device.ZoneName, [device.ZoneName], Convert.ToDouble(device.SetTemp) - 0.5, 1, stoppingToken);
            }
        }
    }
}
