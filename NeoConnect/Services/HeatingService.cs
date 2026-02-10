
using System;

namespace NeoConnect
{
    public class HeatingService : IHeatingService
    {
        private readonly ILogger<HeatingService> _logger;
        private readonly INeoHubService _neoHub;
        private readonly IEmailService _emailService;
        private readonly IDataService _dataService;
        private readonly InMemoryDataService _inMemoryDataService;

        public HeatingService(ILogger<HeatingService> logger, INeoHubService neoHub, IEmailService emailService, IDataService dataService, InMemoryDataService inMemoryDataService)
        {
            _logger = logger;
            _neoHub = neoHub;
            _emailService = emailService;
            _dataService = dataService;
            _inMemoryDataService = inMemoryDataService;
        }

        /// <summary>
        /// Gets live device data from the NeoHub
        /// </summary>
        /// <param name="includeAdvancedData">Flag to indicate whether to return ROC, Max Preheat and other advanced data.</param>
        /// <param name="stoppingToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns></returns>
        public async Task<IEnumerable<Device>> GetDevices(bool includeAdvancedData, CancellationToken stoppingToken)
        {            
            Dictionary<string, int> rocData = null;
            Dictionary<string, EngineersData> engineersData = null;
            List<NeoDevice> neoDevices;

            using (var connection = await _neoHub.CreateConnection(stoppingToken))
            {                
                neoDevices = await _neoHub.GetDevices(connection, stoppingToken);

                _inMemoryDataService.CacheDeviceNames(neoDevices.ToDictionary(d => d.DeviceId, d => d.ZoneName));

                if (includeAdvancedData)
                {
                    rocData = await _neoHub.GetROCData(connection, neoDevices.Select(d => d.ZoneName).ToArray(), stoppingToken);
                    engineersData = await _neoHub.GetEngineersData(connection, CancellationToken.None);
                }
            }

            var devices = new List<Device>();

            foreach (var neoDevice in neoDevices)
            {
                var device = Device.FromNeoDevice(neoDevice);
                                                        
                if (rocData != null && rocData.TryGetValue(device.ZoneName, out int roc))
                {
                    device.RoC = roc;
                }

                if (engineersData != null && engineersData.TryGetValue(device.ZoneName, out EngineersData eng))
                {
                    device.MaxPreheatHours = eng.MaxPreheatDuration;
                }

                device.ProfileName = _inMemoryDataService.GetProfileName(neoDevice.ActiveProfile);                

                devices.Add(device);
            }
            
            return devices;
        }

        /// <summary>
        /// Gets device heating history data for the given day
        /// </summary>
        /// <param name="date">THe date to retrieve history data for.</param>
        /// <param name="stoppingToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns></returns>
        public async Task<IEnumerable<DeviceHistory>> GetDeviceHistory(DateTime date, CancellationToken stoppingToken)
        {
            var data = await _dataService.GetDeviceData(date);
            var devices = data.GroupBy(d => d.DeviceId).ToArray();

            // build the grid            
            var grid = new List<DeviceHistory>();

            foreach (var device in devices)
            {
                var gridItem = new DeviceHistory();
                gridItem.DeviceName = _inMemoryDataService.GetDeviceName(device.Key);

                int j = 0;
                var deviceData = device.OrderBy(d => d.Timestamp);
                int lastIdx = -1;
                foreach (var val in deviceData)
                {
                    var nextIdx = GetIndex(val.Timestamp);


                    // Fill gaps
                    while (j < nextIdx)
                    {
                        gridItem.History[j++] = -1;
                    }

                    gridItem.History[nextIdx] = val.PreheatActive ? 2 : val.HeatOn ? 1 : 0;
                    if (nextIdx != lastIdx) // Don't let duplicate entries for same index throw out our sequence!
                        j++;
                    lastIdx = nextIdx;
                }

                // Fill remaining
                while (j < 96)
                {
                    gridItem.History[j++] = -1;
                }

                grid.Add(gridItem);
            }

            return grid;
        }

        /// <summary>
        /// Gets schedule data from the NeoHub
        /// </summary>
        /// <param name="stoppingToken">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns></returns>
        public async Task<IEnumerable<Schedule>> GetSchedules(CancellationToken stoppingToken)
        {
            var schedules = new List<Schedule>();

            using (var connection = await _neoHub.CreateConnection(stoppingToken))
            {
                var profiles = await _neoHub.GetAllProfiles(connection, stoppingToken);

                _inMemoryDataService.CacheProfileNames(profiles.Select(p => new KeyValuePair<int, string>(p.Value.ProfileId, p.Value.ProfileName)).ToDictionary());

                foreach (var profile in profiles)
                {
                    var comfortLevels = new ComfortLevel[8];
                    comfortLevels[0] = new ComfortLevel(profile.Value.Schedule.Weekdays.Wake);
                    comfortLevels[1] = new ComfortLevel(profile.Value.Schedule.Weekdays.Leave);
                    comfortLevels[2] = new ComfortLevel(profile.Value.Schedule.Weekdays.Return);
                    comfortLevels[3] = new ComfortLevel(profile.Value.Schedule.Weekdays.Sleep);
                    comfortLevels[4] = new ComfortLevel(profile.Value.Schedule.Weekends.Wake);
                    comfortLevels[5] = new ComfortLevel(profile.Value.Schedule.Weekends.Leave);
                    comfortLevels[6] = new ComfortLevel(profile.Value.Schedule.Weekends.Return);
                    comfortLevels[7] = new ComfortLevel(profile.Value.Schedule.Weekends.Sleep);

                    schedules.Add(new Schedule() { ScheduleName = profile.Value.ProfileName, Intervals = comfortLevels });
                }
            }

            return schedules;
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

            using (var connection = await _neoHub.CreateConnection(stoppingToken))
            {
                var devices = await _neoHub.GetDevices(connection, stoppingToken);
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

                var profiles = await _neoHub.GetAllProfiles(connection, stoppingToken);
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
                    await _neoHub.Boost(connection, [timer.ZoneName], 1, stoppingToken);
                    await _emailService.SendInfoEmail($"Boosted {timer.ZoneName}", stoppingToken);
                }
                else
                {
                    _logger.LogInformation($"Bathroom Boost not required this time. Bathroom is currently {System.Math.Abs(temperatureDifference)}c {(temperatureDifference < 0 ? "above" : "below")} target.");
                }
            }
        }


        /// <summary>
        /// If it is X degrees or more when this method runs, it will turn all stats down by half a degree for 1 hour because the sun will provide additional warming during this time.
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

            using (var connection = await _neoHub.CreateConnection(stoppingToken))
            {
                // fetch all the necessary data from the NeoHub
                var devices = (await _neoHub.GetDevices(connection, stoppingToken)).Where(d => d.IsThermostat && !d.IsOffline && d.ActiveProfile != 0 && !d.IsStandby);

                var holdGroup = "ReduceWhenWarm";
                foreach (var device in devices)
                {
                    await _neoHub.Hold(connection, holdGroup, [device.ZoneName], Convert.ToDouble(device.SetTemp) - 0.5, 1, stoppingToken);
                }
                await _emailService.SendInfoEmail(devices.Select(d => $"Holding {d.ZoneName} down 0.5c for 1 hour"), stoppingToken);
            }
        }

        public async Task LogDeviceStatuses(CancellationToken stoppingToken)
        {
            using (var connection = await _neoHub.CreateConnection(stoppingToken))
            {
                var devices = (await _neoHub.GetDevices(connection, stoppingToken)).Where(d => !d.IsOffline && d.ActiveProfile != 0 && !d.IsStandby);

                _logger.LogInformation($"Writing device statuses to database.");
                _dataService.AddDeviceData(devices, 0);
            }
        }

        private int GetIndex(DateTime timestamp)
        {
            return (timestamp.Hour * 4) + (timestamp.Minute < 15 ? 0 : timestamp.Minute < 30 ? 1 : timestamp.Minute < 45 ? 2 : 3);
        }
    }
}
