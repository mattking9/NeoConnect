using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

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
            _mockLogger = new Mock<ILogger<HeatingService>>();
            
            _config = new HeatingConfig
            {
                SummerProfileName = "Summer",
                PreHeatOverride = new PreHeatOverrideConfig
                {
                    DefaultPreheatDuration = 2,
                    ExternalTempThresholdForCancel = 18.0m,
                    MaxTempDifferenceForCancel = 2.0m
                },
                Recipes = new RecipeConfig
                {
                    ExternalTempThreshold = 15.0m,
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
        public async Task SetPreheatDurationBasedOnWeatherConditions_DeviceIsNotThermostat_SkipsDevice()
        {
            // Arrange
            var forecastToday = CreateForecastHours(20.0m);

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
                    1, new Profile { ProfileId = 1, ProfileName = "Winter" }
                }
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, EngineersData>());

            // Act
            await _heatingService.SetPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            Assert.That(_heatingService.GetChangesMade().Count == 0);
            _mockNeoHubService.Verify(s => s.SetPreheatDuration(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_DeviceIsOffline_SkipsDevice()
        {
            // Arrange
            var forecastToday = CreateForecastDay(20.0m);

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
                    1, new Profile { ProfileId = 1, ProfileName = "Winter" }
                }
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, EngineersData>());

            // Act
            await _heatingService.SetPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            Assert.That(_heatingService.GetChangesMade().Count == 0);
            _mockNeoHubService.Verify(s => s.SetPreheatDuration(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_NoProfileForDevice_SkipsDevice()
        {
            // Arrange
            var forecastToday = CreateForecastHours(20.0m);

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
            await _heatingService.SetPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            Assert.That(_heatingService.GetChangesMade().Count == 0);
            _mockNeoHubService.Verify(s => s.SetPreheatDuration(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_DeviceInSummerMode_SkipsDevice()
        {
            // Arrange
            var forecastToday = CreateForecastHours(20.0m);
            
            var devices = new List<NeoDevice>
            {
                new NeoDevice
                {
                    ZoneName = "Lounge",
                    IsThermostat = true,
                    IsOffline = false,
                    ActiveProfile = 1
                }
            };            
                        
            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1, ProfileName = "Summer" }  // Same as _config.SummerProfileName
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
            await _heatingService.SetPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            Assert.That(_heatingService.GetChangesMade().Count == 0);
            _mockNeoHubService.Verify(s => s.SetPreheatDuration(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_NoNextInterval_SkipsDevice()
        {
            // Arrange
            var forecastToday = CreateForecastHours(20.0m);

            var devices = new List<NeoDevice>
            {
                new NeoDevice
                {
                    ZoneName = "Lounge",
                    IsThermostat = true,
                    IsOffline = false,
                    ActiveProfile = 1
                }
            };

            ComfortLevel nextInterval = null;

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1, ProfileName = "Winter" }
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
            await _heatingService.SetPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            Assert.That(_heatingService.GetChangesMade().Count == 0);
            _mockNeoHubService.Verify(s => s.SetPreheatDuration(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_HighExternalTempAndRoomTempNearTarget_PreheatSet_CancelsPreheat()
        {
            // Arrange
            var forecastToday = CreateForecastHours(10.0m, 20.0m); // Hour 0 = 10.0, Hour 1 = 20.0 (above threshold)

            var nextInterval = new ComfortLevel(new object[]{ "01:00:00", "21" });

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1, ProfileName = "Winter" }
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
                    ActualTemp = "19.5" // Close to target (21.0 - 19.5 = 1.5 which is < MaxTempDifferenceForCancel of 2.0)
                }
            };

            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Lounge", new EngineersData { MaxPreheatDuration = 2 } } // Preheat duration set to 2 hours
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(engineersData);
            _mockNeoHubService.Setup(s => s.GetNextSwitchingInterval(It.IsAny<ProfileSchedule>(), It.IsAny<DateTime?>())).Returns(nextInterval);

            // Act
            await _heatingService.SetPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.SetPreheatDuration("Lounge", 0, It.IsAny<CancellationToken>()), Times.Once);
            
            var changes = _heatingService.GetChangesMade();
            Assert.That(changes, Contains.Item("Lounge preheat duration was changed to 0 hours."));
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_HighExternalTempAndRoomTempNearTarget_PreheatAlreadyCancelled_DoesNotCancelPreheat()
        {
            // Arrange
            var forecastToday = CreateForecastHours(10.0m, 20.0m); // Hour 0 = 10.0, Hour 1 = 20.0 (above threshold)

            var nextInterval = new ComfortLevel(new object[] { "01:00:00", "21" });

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1, ProfileName = "Winter" }
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
                    ActualTemp = "19.5" // Close to target (21.0 - 19.5 = 1.5 which is < MaxTempDifferenceForCancel of 2.0)
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
            await _heatingService.SetPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.SetPreheatDuration("Lounge", 0, It.IsAny<CancellationToken>()), Times.Never);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes.Count == 0);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_LowExternalTemp_PreheatCancelled_SetsPreheat()
        {
            // Arrange
            var forecastToday = CreateForecastHours(10.0m, 10.0m); // Hour 0 = 10.0, Hour 1 = 10.0 (below threshold)

            var nextInterval = new ComfortLevel(new object[] { "01:00:00", "21" });

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1, ProfileName = "Winter" }
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
                    ActualTemp = "19.5" // Close to target (21.0 - 19.5 = 1.5 which is < MaxTempDifferenceForCancel of 2.0)
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
            await _heatingService.SetPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.SetPreheatDuration("Lounge", 2, It.IsAny<CancellationToken>()), Times.Once);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes.Count == 1);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_LowExternalTemp_PreheatAlreadySet_DoesNotSetPreheat()
        {
            // Arrange
            var forecastToday = CreateForecastHours(10.0m, 10.0m); // Hour 0 = 10.0, Hour 1 = 10.0 (below threshold)

            var nextInterval = new ComfortLevel(new object[] { "01:00:00", "21" });

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1, ProfileName = "Winter" }
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
                    ActualTemp = "19.5" // Close to target (21.0 - 19.5 = 1.5 which is < MaxTempDifferenceForCancel of 2.0)
                }
            };

            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Lounge", new EngineersData { MaxPreheatDuration = 2 } } // Preheat duration set to 2 hours
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(engineersData);
            _mockNeoHubService.Setup(s => s.GetNextSwitchingInterval(It.IsAny<ProfileSchedule>(), It.IsAny<DateTime?>())).Returns(nextInterval);

            // Act
            await _heatingService.SetPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.SetPreheatDuration("Lounge", 0, It.IsAny<CancellationToken>()), Times.Never);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes.Count == 0);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_TempNotNearTarget_PreheatCancelled_SetsPreheat()
        {
            // Arrange
            var forecastToday = CreateForecastHours(10.0m, 20.0m); // Hour 0 = 10.0, Hour 1 = 20.0 (above threshold)

            var nextInterval = new ComfortLevel(new object[] { "01:00:00", "21" });

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1, ProfileName = "Winter" }
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
                    ActualTemp = "18.5" // Not close to target (21.0 - 18.5 = 2.5 which is > MaxTempDifferenceForCancel of 2.0)
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
            await _heatingService.SetPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.SetPreheatDuration("Lounge", 2, It.IsAny<CancellationToken>()), Times.Once);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes.Count == 1);
        }

        [Test]
        public async Task SetPreheatDurationBasedOnWeatherConditions_TempNotNearTarget_PreheatAlreadySet_DoesNotSetPreheat()
        {
            // Arrange
            var forecastToday = CreateForecastHours(10.0m, 20.0m); // Hour 0 = 10.0, Hour 1 = 20.0 (above threshold)


            var nextInterval = new ComfortLevel(new object[] { "01:00:00", "21" });

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1, ProfileName = "Winter" }
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
                    ActualTemp = "18.5" // Not close to target (21.0 - 18.5 = 2.5 which is > MaxTempDifferenceForCancel of 2.0)
                }
            };

            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Lounge", new EngineersData { MaxPreheatDuration = 2 } } // Preheat duration set to 2 hours
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(engineersData);
            _mockNeoHubService.Setup(s => s.GetNextSwitchingInterval(It.IsAny<ProfileSchedule>(), It.IsAny<DateTime?>())).Returns(nextInterval);

            // Act
            await _heatingService.SetPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.SetPreheatDuration("Lounge", 0, It.IsAny<CancellationToken>()), Times.Never);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes.Count == 0);
        }

        [Test]
        public async Task RunRecipeBasedOnWeatherConditions_WhenWarm_RunsSummerRecipe()
        {
            // Arrange
            var forecastToday = CreateForecastDay(20.0m); // Above threshold                        

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
            var forecastToday = CreateForecastDay(10.0m); // Below threshold                        

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
            var forecastToday = CreateForecastDay(10.0m); // Below threshold

            // Act
            await _heatingService.RunRecipeBasedOnWeatherConditions(forecastToday, _cts.Token); // First run, should run Winter Recipe
            await _heatingService.RunRecipeBasedOnWeatherConditions(forecastToday, _cts.Token); // Second run, should not run again

            // Assert
            _mockNeoHubService.Verify(s => s.RunRecipe("Winter Recipe", It.IsAny<CancellationToken>()), Times.Once);

            var changes = _heatingService.GetChangesMade();
            Assert.That(changes.Count == 1);
        }
        
        // Helper methods for creating test data
        private ForecastDay CreateForecastDay(decimal averageTemp)
        {
            return new ForecastDay
            {
                Day = new ForecastDayDaily { AverageTemp = averageTemp },
            };
        }
        
        private ForecastDay CreateForecastHours(params decimal[] hourlyTemps)
        {
            return new ForecastDay
            {
                Hour = hourlyTemps.Select(x => new ForecastHour { Temp = x }).ToList()
            };                     
        }
    }
}