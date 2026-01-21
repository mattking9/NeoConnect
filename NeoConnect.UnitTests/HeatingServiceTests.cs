using Microsoft.Extensions.Logging;
using Moq;

namespace NeoConnect.UnitTests
{
    [TestFixture]
    public class HeatingServiceTests
    {
        private HeatingService _heatingService;
        private Mock<INeoHubService> _mockNeoHubService;
        private Mock<IEmailService> _mockEmailService;
        private Mock<IReportDataService> _mockReportDataService;
        private Mock<ILogger<HeatingService>> _mockLogger;
        private CancellationTokenSource _cts;

        [SetUp]
        public void Setup()
        {
            _mockNeoHubService = new Mock<INeoHubService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockReportDataService = new Mock<IReportDataService>();
            _mockLogger = new Mock<ILogger<HeatingService>>();
            _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _cts = new CancellationTokenSource();
            _heatingService = new HeatingService(_mockLogger.Object, _mockNeoHubService.Object, _mockEmailService.Object, _mockReportDataService.Object);
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
        public async Task BoostTowelRailWhenBathroomIsCold_Boosts_WhenTempIsBelowTarget()
        {
            // Arrange
            var bathroom = new NeoDevice
            {
                ZoneName = "Bathroom",
                IsOffline = false,
                IsStandby = false,
                SetTemp = "20",
                ActualTemp = "18",
                ActiveProfile = 1
            };
            var towelRail = new NeoDevice
            {
                ZoneName = "Towel Rail"
            };
            var devices = new List<NeoDevice> { bathroom, towelRail };
            var schedule = new ProfileSchedule();
            var profile = new Profile { Schedule = schedule };
            var profiles = new Dictionary<int, Profile> { { 1, profile } };
            var comfortLevel = new ComfortLevel(new object[] { "01:00:00", "20" });

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetNextComfortLevel(schedule, It.IsAny<DateTime>())).Returns(comfortLevel);

            // Act
            await _heatingService.BoostTowelRailWhenBathroomIsCold(_cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.Boost(new[] { "Towel Rail" }, 1, It.IsAny<CancellationToken>()), Times.Once);
            _mockEmailService.Verify(e => e.SendInfoEmail(It.Is<string>(str => str.Contains("Boosted")), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task BoostTowelRailWhenBathroomIsCold_DoesNotBoost_WhenTempIsNotBelowTarget()
        {
            // Arrange
            var bathroom = new NeoDevice
            {
                ZoneName = "Bathroom",
                IsOffline = false,
                IsStandby = false,
                SetTemp = "20",
                ActualTemp = "19.5",
                ActiveProfile = 1
            };
            var towelRail = new NeoDevice
            {
                ZoneName = "Towel Rail"
            };
            var devices = new List<NeoDevice> { bathroom, towelRail };
            var schedule = new ProfileSchedule();
            var profile = new Profile { Schedule = schedule };
            var profiles = new Dictionary<int, Profile> { { 1, profile } };
            var comfortLevel = new ComfortLevel(new object[] { "01:00:00", "20" });

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetNextComfortLevel(schedule, It.IsAny<DateTime>())).Returns(comfortLevel);

            // Act
            await _heatingService.BoostTowelRailWhenBathroomIsCold(_cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.Boost(It.IsAny<string[]>(), 1, It.IsAny<CancellationToken>()), Times.Never);
            _mockEmailService.Verify(e => e.SendInfoEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task ReduceSetTempWhenExternalTempIsWarm_HoldsDevices_WhenTempIsAboveThreshold()
        {
            // Arrange
            var forecastDay = new ForecastDay
            {
                Hour = Enumerable.Range(0, 24).Select(i => new ForecastHour { Temp = 15 }).ToList()
            };
            var devices = new List<NeoDevice>
            {
                new NeoDevice { ZoneName = "Zone1", IsThermostat = true, IsOffline = false, IsStandby = false, ActiveProfile = 1, SetTemp = "21" },
                new NeoDevice { ZoneName = "Zone2", IsThermostat = true, IsOffline = false, IsStandby = false, ActiveProfile = 2, SetTemp = "20" }
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            // Act
            await _heatingService.ReduceSetTempWhenExternalTempIsWarm(forecastDay, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.Hold("ReduceWhenWarm", It.IsAny<string[]>() , It.IsAny<double>(), 1, It.IsAny<CancellationToken>()), Times.Exactly(devices.Count));
            _mockEmailService.Verify(e => e.SendInfoEmail(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ReduceSetTempWhenExternalTempIsWarm_DoesNothing_WhenTempIsBelowThreshold()
        {
            // Arrange
            var forecastDay = new ForecastDay
            {
                Hour = Enumerable.Range(0, 24).Select(i => new ForecastHour { Temp = 10 }).ToList()
            };

            var devices = new List<NeoDevice>
            {
                new NeoDevice { ZoneName = "Zone1", IsThermostat = true, IsOffline = false, IsStandby = false, ActiveProfile = 1, SetTemp = "21" },
                new NeoDevice { ZoneName = "Zone2", IsThermostat = true, IsOffline = false, IsStandby = false, ActiveProfile = 2, SetTemp = "20" }
            };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            // Act
            await _heatingService.ReduceSetTempWhenExternalTempIsWarm(forecastDay, _cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.Hold(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<double>(), 1, It.IsAny<CancellationToken>()), Times.Never);
            _mockEmailService.Verify(e => e.SendInfoEmail(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        }        

        [Test]
        public async Task BoostTowelRailWhenBathroomIsCold_LogsAndReturns_WhenBathroomDeviceNotFound()
        {
            // Arrange
            var towelRail = new NeoDevice { ZoneName = "Towel Rail" };
            var devices = new List<NeoDevice> { towelRail };
            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            // Act
            await _heatingService.BoostTowelRailWhenBathroomIsCold(_cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.Boost(It.IsAny<string[]>(), 1, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task BoostTowelRailWhenBathroomIsCold_LogsAndReturns_WhenTowelRailDeviceNotFound()
        {
            // Arrange
            var bathroom = new NeoDevice { ZoneName = "Bathroom", IsOffline = false, IsStandby = false, SetTemp = "20", ActualTemp = "18", ActiveProfile = 1 };
            var devices = new List<NeoDevice> { bathroom };
            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            // Act
            await _heatingService.BoostTowelRailWhenBathroomIsCold(_cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.Boost(It.IsAny<string[]>(), 1, It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestCase(true, false, "20", "18")] // IsOffline
        [TestCase(false, true, "20", "18")] // IsStandby
        [TestCase(false, false, "12", "10")] // SetTemp <= 12
        public async Task BoostTowelRailWhenBathroomIsCold_LogsAndReturns_WhenBathroomInactive(bool isOffline, bool isStandby, string setTemp, string actualTemp)
        {
            // Arrange
            var bathroom = new NeoDevice { ZoneName = "Bathroom", IsOffline = isOffline, IsStandby = isStandby, SetTemp = setTemp, ActualTemp = actualTemp, ActiveProfile = 1 };
            var towelRail = new NeoDevice { ZoneName = "Towel Rail" };
            var devices = new List<NeoDevice> { bathroom, towelRail };
            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            // Act
            await _heatingService.BoostTowelRailWhenBathroomIsCold(_cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.Boost(It.IsAny<string[]>(), 1, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task BoostTowelRailWhenBathroomIsCold_LogsAndReturns_WhenNoComfortLevel()
        {
            // Arrange
            var bathroom = new NeoDevice { ZoneName = "Bathroom", IsOffline = false, IsStandby = false, SetTemp = "20", ActualTemp = "18", ActiveProfile = 1 };
            var towelRail = new NeoDevice { ZoneName = "Towel Rail" };
            var devices = new List<NeoDevice> { bathroom, towelRail };
            var schedule = new ProfileSchedule();
            var profile = new Profile { Schedule = schedule };
            var profiles = new Dictionary<int, Profile> { { 1, profile } };

            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(s => s.GetNextComfortLevel(schedule, It.IsAny<DateTime>())).Returns((ComfortLevel?)null);

            // Act
            await _heatingService.BoostTowelRailWhenBathroomIsCold(_cts.Token);

            // Assert
            _mockNeoHubService.Verify(s => s.Boost(It.IsAny<string[]>(), 1, It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}