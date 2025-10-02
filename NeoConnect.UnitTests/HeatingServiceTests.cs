using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NeoConnect.Services;

namespace NeoConnect.UnitTests
{
    [TestFixture]
    public class HeatingServiceTests
    {
        private HeatingService _heatingService;
        private Mock<INeoHubService> _mockNeoHubService;
        private Mock<ILogger<HeatingService>> _mockLogger;
        private Mock<IOptions<HeatingConfig>> _mockOptions;
        private HeatingConfig _config;
        private CancellationTokenSource _cts;

        [SetUp]
        public void Setup()
        {
            _mockNeoHubService = new Mock<INeoHubService>();

            var rocMap = new Dictionary<string, int>
            {                
                { "Lounge", 60 }
            };
            _mockNeoHubService.Setup(n => n.GetROCData(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(rocMap);

            _mockLogger = new Mock<ILogger<HeatingService>>();
            _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            _config = new HeatingConfig
            {
                PreHeatOverride = new PreHeatOverrideConfig
                {
                    MaxPreheatHours = 3,
                    OnlyEnablePreheatForWakeSchedules = false,
                },
                Recipes = new RecipeConfig
                {
                    ExternalTempThreshold = 15.0,
                    SummerRecipeName = "Summer Recipe",
                    WinterRecipeName = "Winter Recipe"
                }
            };



            _mockOptions = new Mock<IOptions<HeatingConfig>>();
            _mockOptions.Setup(o => o.Value).Returns(_config);

            _cts = new CancellationTokenSource();

            _heatingService = new HeatingService(
                _mockOptions.Object,
                _mockLogger.Object,
                _mockNeoHubService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _cts.Dispose();
        }

        [Test]
        public async Task Init_CallsNeoHubConnect()
        {
            // Arrange
            _mockNeoHubService.Setup(s => s.Connect(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            await _heatingService.Init(_cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.Connect(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Cleanup_CallsNeoHubDisconnect()
        {
            // Arrange
            _mockNeoHubService.Setup(s => s.Disconnect(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            await _heatingService.Cleanup(_cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.Disconnect(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_ActionNotEnabled_DoesNotRun()
        {
            // Arrange
            _config.PreHeatOverride.Enabled = false;

            // Act
            await _heatingService.SetMaxPreheatDurationBasedOnWeatherConditions(null, _cts.Token);

            // Assert
            Assert.That(_heatingService.GetChangesMade().Count == 0);
            _mockNeoHubService.Verify(s => s.SetPreheatDuration(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_DeviceIsNotThermostat_SkipsDevice()
        {
            // Arrange
            var forecastToday = CreateForecastHours(false, false, 20.0);

            var devices = new List<NeoDevice>
            {
                new NeoDevice
                {
                    ZoneName = "Lounge",
                    IsThermostat = false,
                    IsOffline = false,
                    ActiveProfile = 1
                }
            };

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1 }
                }
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, EngineersData>());

            // Act
            await _heatingService.SetMaxPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            Assert.That(_heatingService.GetChangesMade().Count == 0);
            _mockNeoHubService.Verify(s => s.SetPreheatDuration(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_DeviceIsOffline_SkipsDevice()
        {
            // Arrange
            var forecastToday = CreateForecastHours(false, false, 20.0);

            var devices = new List<NeoDevice>
            {
                new NeoDevice
                {
                    ZoneName = "Lounge",
                    IsThermostat = true,
                    IsOffline = true,
                    ActiveProfile = 1
                }
            };

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1 }
                }
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, EngineersData>());

            // Act
            await _heatingService.SetMaxPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            Assert.That(_heatingService.GetChangesMade().Count == 0);
            _mockNeoHubService.Verify(s => s.SetPreheatDuration(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_NoProfileForDevice_SkipsDevice()
        {
            // Arrange
            var forecastToday = CreateForecastHours(false, false, 20.0);

            var devices = new List<NeoDevice>
            {
                new NeoDevice
                {
                    ZoneName = "Lounge",
                    IsThermostat = true,
                    IsOffline = false,
                    ActiveProfile = 0
                }
            };

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1, ProfileName = "X" }
                }
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, EngineersData>());

            // Act
            await _heatingService.SetMaxPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            Assert.That(_heatingService.GetChangesMade().Count == 0);
            _mockNeoHubService.Verify(s => s.SetPreheatDuration(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_DeviceInStandByMode_SkipsDevice()
        {
            // Arrange
            var forecastToday = CreateForecastHours(false, false, 20.0);

            var devices = new List<NeoDevice>
            {
                new NeoDevice
                {
                    ZoneName = "Lounge",
                    IsThermostat = true,
                    IsOffline = false,
                    IsStandby = true, // StandBy mode
                    ActiveProfile = 1,
                    SetTemp = "16"
                }
            };

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1 } 
                }
            };

            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Lounge", new EngineersData { MaxPreheatDuration = 2 } }
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(engineersData);

            // Act
            await _heatingService.SetMaxPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            Assert.That(_heatingService.GetChangesMade().Count == 0);
            _mockNeoHubService.Verify(s => s.SetPreheatDuration(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_DeviceHasAntiFrostTempSet_SkipsDevice()
        {
            // Arrange
            var forecastToday = CreateForecastHours(false, false, 20.0);

            var devices = new List<NeoDevice>
            {
                new NeoDevice
                {
                    ZoneName = "Lounge",
                    IsThermostat = true,
                    IsOffline = false,
                    ActiveProfile = 1,
                    SetTemp = "12" // At frost temperature
                }
            };

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1 }
                }
            };

            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Lounge", new EngineersData { MaxPreheatDuration = 2, FrostTemp = 12 } }
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(engineersData);

            // Act
            await _heatingService.SetMaxPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            Assert.That(_heatingService.GetChangesMade().Count == 0);
            _mockNeoHubService.Verify(s => s.SetPreheatDuration(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_NoNextInterval_SkipsDevice()
        {
            // Arrange
            var forecastToday = CreateForecastHours(false, false, 20.0);

            var devices = new List<NeoDevice>
            {
                new NeoDevice
                {
                    ZoneName = "Lounge",
                    IsThermostat = true,
                    IsOffline = false,
                    ActiveProfile = 1,
                    SetTemp = "16"
                }
            };

            ComfortLevel nextInterval = null;

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1 }
                }
            };

            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Lounge", new EngineersData { MaxPreheatDuration = 2 } }
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(engineersData);
            _mockNeoHubService.Setup(s => s.GetNextSwitchingInterval(It.IsAny<ProfileSchedule>(), It.IsAny<DateTime?>())).Returns(nextInterval);

            // Act
            await _heatingService.SetMaxPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            Assert.That(_heatingService.GetChangesMade().Count == 0);
            _mockNeoHubService.Verify(s => s.SetPreheatDuration(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_PreheatAlreadySetToValue_DoesNotSetValue()
        {
            // Arrange
            var forecastToday = CreateForecastHours(false, false, 10.0, 10.5, 10.7);

            var nextInterval = new ComfortLevel(new object[] { "01:00:00", "21" });

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1 }
                }
            };

            var devices = new List<NeoDevice>
            {
                new NeoDevice
                {
                    ZoneName = "Lounge",
                    IsThermostat = true,
                    IsOffline = false,
                    ActiveProfile = 1,
                    SetTemp = "16",
                    ActualTemp = "19" //at ROC of 60, preheat duration should be 2 hours
                }
            };

            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Lounge", new EngineersData { MaxPreheatDuration = 2 } } // Preheat duration already set to 2 hours
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(engineersData);
            _mockNeoHubService.Setup(s => s.GetNextSwitchingInterval(It.IsAny<ProfileSchedule>(), It.IsAny<DateTime?>())).Returns(nextInterval);

            // Act
            await _heatingService.SetMaxPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.SetPreheatDuration("Lounge", It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes.Count == 0);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_NoExternalTemperatureWeightings_PreheatNotAlreadySetToValue_SetsValue()
        {
            // Arrange
            var forecastToday = CreateForecastHours(false, false, 10.0, 10.5, 10.7);

            var nextInterval = new ComfortLevel(new object[] { "01:00:00", "21" });

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1 }
                }
            };

            var devices = new List<NeoDevice>
            {
                new NeoDevice
                {
                    ZoneName = "Lounge",
                    IsThermostat = true,
                    IsOffline = false,
                    ActiveProfile = 1,
                    ActualTemp = "19", //at ROC of 60, preheat duration should be 2 hours
                    SetTemp = "16"
                }
            };

            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Lounge", new EngineersData { MaxPreheatDuration = 0 } } // Preheat duration currently set to 0 hours
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(engineersData);
            _mockNeoHubService.Setup(s => s.GetNextSwitchingInterval(It.IsAny<ProfileSchedule>(), It.IsAny<DateTime?>())).Returns(nextInterval);

            // Act
            await _heatingService.SetMaxPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.SetPreheatDuration("Lounge", 2, It.IsAny<CancellationToken>()), Times.Once);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes.Count == 1);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_PreheatIsBelowMaximumAllowed_DoesNotExceedMaximum()
        {
            // Arrange            
            var forecastToday = CreateForecastHours(false, false, 1.0, 1.0, 1.0); // Below the lowest weighting of 6.0

            var nextInterval = new ComfortLevel(new object[] { "01:00:00", "21" });

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1 }
                }
            };

            var devices = new List<NeoDevice>
            {
                new NeoDevice
                {
                    ZoneName = "Lounge",
                    IsThermostat = true,
                    IsOffline = false,
                    ActiveProfile = 1,
                    ActualTemp = "15",  //at ROC of 60, preheat duration should be 6 hours
                    SetTemp = "15"
                }
            };

            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Lounge", new EngineersData { MaxPreheatDuration = -1 } }
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(engineersData);
            _mockNeoHubService.Setup(s => s.GetNextSwitchingInterval(It.IsAny<ProfileSchedule>(), It.IsAny<DateTime?>())).Returns(nextInterval);

            // Act
            await _heatingService.SetMaxPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.SetPreheatDuration("Lounge", 3, It.IsAny<CancellationToken>()), Times.Once);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes.Count == 1);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_TempIsBelowLowestWeighting_DoesNotApplyAnyWeighting()
        {
            // Arrange
            _config.PreHeatOverride.ExternalTempROCWeightings = new List<TemperatureWeighting>()
            {
                new TemperatureWeighting { Temp = 6.0, Weighting = 0.67 },
                new TemperatureWeighting { Temp = 9.0, Weighting = 0.33 }
            };

            var forecastToday = CreateForecastHours(false, false, 1.0, 1.0, 1.0); // Below the lowest weighting of 6.0

            var nextInterval = new ComfortLevel(new object[] { "01:00:00", "21" });

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1 }
                }
            };

            var devices = new List<NeoDevice>
            {
                new NeoDevice
                {
                    ZoneName = "Lounge",
                    IsThermostat = true,
                    IsOffline = false,
                    ActiveProfile = 1,
                    ActualTemp = "19",  //at ROC of 60, preheat duration should be 2 hours
                    SetTemp = "16"
                }
            };

            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Lounge", new EngineersData { MaxPreheatDuration = -1 } }
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(engineersData);
            _mockNeoHubService.Setup(s => s.GetNextSwitchingInterval(It.IsAny<ProfileSchedule>(), It.IsAny<DateTime?>())).Returns(nextInterval);

            // Act
            await _heatingService.SetMaxPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.SetPreheatDuration("Lounge", 2, It.IsAny<CancellationToken>()), Times.Once);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes.Count == 1);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_TempIsBetweenWeightings_AppliesCorrectWeighting()
        {
            // Arrange
            _config.PreHeatOverride.ExternalTempROCWeightings = new List<TemperatureWeighting>()
            {
                new TemperatureWeighting { Temp = 6.0, Weighting = 0.67 },
                new TemperatureWeighting { Temp = 9.0, Weighting = 0.33 }
            };

            var forecastToday = CreateForecastHours(false, false, 7.0, 7.0, 7.0); // Between the two weightings of 6.0 and 9.0

            var nextInterval = new ComfortLevel(new object[] { "01:00:00", "21" });

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1 }
                }
            };

            var devices = new List<NeoDevice>
            {
                new NeoDevice
                {
                    ZoneName = "Lounge",
                    IsThermostat = true,
                    IsOffline = false,
                    ActiveProfile = 1,
                    ActualTemp = "19.0", //at ROC of 60, preheat duration should be 2 hours
                    SetTemp = "16"
                }
            };

            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Lounge", new EngineersData { MaxPreheatDuration = 0 } } // Preheat duration set to 0 hours
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(engineersData);
            _mockNeoHubService.Setup(s => s.GetNextSwitchingInterval(It.IsAny<ProfileSchedule>(), It.IsAny<DateTime?>())).Returns(nextInterval);

            // Act
            await _heatingService.SetMaxPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.SetPreheatDuration("Lounge", 2, It.IsAny<CancellationToken>()), Times.Once);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes.Count == 1);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_TempIsEqualToHighestWeighting_AppliesHighestWeighting()
        {
            // Arrange
            _config.PreHeatOverride.ExternalTempROCWeightings = new List<TemperatureWeighting>()
            {
                new TemperatureWeighting { Temp = 6.0, Weighting = 0.67 },
                new TemperatureWeighting { Temp = 9.0, Weighting = 0 }, //weighting of zero will make preheat be zero hours
            };

            var forecastToday = CreateForecastHours(false, false, 9.0, 9.0, 9.0);

            var nextInterval = new ComfortLevel(new object[] { "01:00:00", "21" });

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1 }
                }
            };

            var devices = new List<NeoDevice>
            {
                new NeoDevice
                {
                    ZoneName = "Lounge",
                    IsThermostat = true,
                    IsOffline = false,
                    ActiveProfile = 1,
                    ActualTemp = "19.0", //at ROC of 60, unweighted preheat duration would be 2 hours
                    SetTemp = "16"
                }
            };

            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Lounge", new EngineersData { MaxPreheatDuration = -1 } }
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(engineersData);
            _mockNeoHubService.Setup(s => s.GetNextSwitchingInterval(It.IsAny<ProfileSchedule>(), It.IsAny<DateTime?>())).Returns(nextInterval);

            // Act
            await _heatingService.SetMaxPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.SetPreheatDuration("Lounge", 0, It.IsAny<CancellationToken>()), Times.Once);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes.Count == 1);
        }        

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_TempIsAboveHighestWeighting_AppliesHighestWeighting()
        {
            // Arrange
            _config.PreHeatOverride.ExternalTempROCWeightings = new List<TemperatureWeighting>()
            {
                new TemperatureWeighting { Temp = 6.0, Weighting = 0.67 },
                new TemperatureWeighting { Temp = 9.0, Weighting = 0 }, //weighting of zero will make preheat be zero hours
            };

            var forecastToday = CreateForecastHours(false, false, 15.0, 15.0, 15.0);

            var nextInterval = new ComfortLevel(new object[] { "01:00:00", "21" });

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1 }
                }
            };

            var devices = new List<NeoDevice>
            {
                new NeoDevice
                {
                    ZoneName = "Lounge",
                    IsThermostat = true,
                    IsOffline = false,
                    ActiveProfile = 1,
                    ActualTemp = "19.0", //at ROC of 60, unweighted preheat duration would be 2 hours
                    SetTemp = "16"
                }
            };

            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Lounge", new EngineersData { MaxPreheatDuration = -1 } }
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(engineersData);
            _mockNeoHubService.Setup(s => s.GetNextSwitchingInterval(It.IsAny<ProfileSchedule>(), It.IsAny<DateTime?>())).Returns(nextInterval);

            // Act
            await _heatingService.SetMaxPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.SetPreheatDuration("Lounge", 0, It.IsAny<CancellationToken>()), Times.Once);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes.Count == 1);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_SunnyAspectWeightingExists_AppliesWeighting()
        {
            // Arrange
            _config.PreHeatOverride.SunnyAspectROCWeightings = new List<SunnyAspectWeighting>()
            {                
                new SunnyAspectWeighting { Devices = new string[] { "Lounge" }, Weighting = 0.5 }, //weighting of 0.5 will half preheat duration
            };

            var forecastToday = CreateForecastHours(true, true, 15.0, 15.0, 15.0);

            var nextInterval = new ComfortLevel(new object[] { "01:00:00", "21" });

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1 }
                }
            };

            var devices = new List<NeoDevice>
            {
                new NeoDevice
                {
                    ZoneName = "Lounge",
                    IsThermostat = true,
                    IsOffline = false,
                    ActiveProfile = 1,
                    ActualTemp = "19.0", //at ROC of 60, unweighted preheat duration would be 2 hours
                    SetTemp = "16"
                }
            };

            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Lounge", new EngineersData { MaxPreheatDuration = -1 } }
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(engineersData);
            _mockNeoHubService.Setup(s => s.GetNextSwitchingInterval(It.IsAny<ProfileSchedule>(), It.IsAny<DateTime?>())).Returns(nextInterval);

            // Act
            await _heatingService.SetMaxPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.SetPreheatDuration("Lounge", 1, It.IsAny<CancellationToken>()), Times.Once);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes.Count == 1);
        }

        [Test]
        public async Task RunRecipeBasedOnWeatherConditions_ActionNotEnabled_DoesNotRun()
        {
            // Arrange
            _config.Recipes.Enabled = false;

            // Act
            await _heatingService.RunRecipeBasedOnWeatherConditions(null, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.RunRecipe(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes.Count == 0);
        }

        [Test]
        public async Task RunRecipeBasedOnWeatherConditions_WhenWarm_RunsSummerRecipe()
        {
            // Arrange
            var forecastToday = CreateForecastDay(20.0); // Above threshold                        

            // Act
            await _heatingService.RunRecipeBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.RunRecipe("Summer Recipe", It.IsAny<CancellationToken>()), Times.Once);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes, Contains.Item("Summer Recipe Recipe was run."));
        }

        [Test]
        public async Task RunRecipeBasedOnWeatherConditions_WhenCold_RunsWinterRecipe()
        {
            // Arrange
            var forecastToday = CreateForecastDay(10.0); // Below threshold                        

            // Act
            await _heatingService.RunRecipeBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.RunRecipe("Winter Recipe", It.IsAny<CancellationToken>()), Times.Once);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes, Contains.Item("Winter Recipe Recipe was run."));
        }

        [Test]
        public async Task RunRecipeBasedOnWeatherConditions_WhenSameRecipeAlreadyRun_DoesNotRunAgain()
        {
            // Arrange
            var forecastToday = CreateForecastDay(10); // Below threshold

            // Act
            await _heatingService.RunRecipeBasedOnWeatherConditions(forecastToday, _cts.Token); // First run, should run Winter Recipe
            await _heatingService.RunRecipeBasedOnWeatherConditions(forecastToday, _cts.Token); // Second run, should not run again

            // Assert
            _mockNeoHubService.Verify(s => s.RunRecipe("Winter Recipe", It.IsAny<CancellationToken>()), Times.Once);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes.Count == 1);
        }

        // Helper methods for creating test data
        private ForecastDay CreateForecastDay(double averageTemp)
        {
            return new ForecastDay
            {
                Day = new ForecastDayDaily { AverageTemp = averageTemp },
            };
        }

        private ForecastDay CreateForecastHours(bool isDaytime, bool isSunny, params double[] hourlyTemps)
        {
            return new ForecastDay
            {
                Hour = hourlyTemps.Select(x => new ForecastHour 
                { 
                    Temp = x, 
                    IsDaytime = isDaytime ? 1 : 0, 
                    Condition = new ForecastCondition() 
                    {
                        Code = isSunny ? 1000 : 1001,
                        Text = isSunny ? "Sunny" : "Partly Cloudy",
                        Icon = isSunny ? "//cdn.weatherapi.com/weather/64x64/day/113.png" : "//cdn.weatherapi.com/weather/64x64/day/116.png"
                    }
                }).ToList()
            };
        }
    }
}