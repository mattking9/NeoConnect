using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NeoConnect.UnitTests
{
    [TestFixture]
    public class GlobalHoldActionTests
    {
        private Mock<IConfiguration> _mockConfig;
        private Mock<IEmailService> _emailService;
        private Mock<ILogger<GlobalHoldAction>> _mockLogger;
        private Mock<IServiceScopeFactory> _mockScopeFactory;
        private Mock<IServiceScope> _mockScope;
        private Mock<IServiceProvider> _mockProvider;
        private Mock<IHeatingService> _mockHeatingService;
        private Mock<IWeatherService> _mockWeatherService;
        private GlobalHoldAction _action;

        [SetUp]
        public void Setup()
        {
            _mockConfig = new Mock<IConfiguration>();
            _emailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<GlobalHoldAction>>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockScope = new Mock<IServiceScope>();
            _mockProvider = new Mock<IServiceProvider>();
            _mockHeatingService = new Mock<IHeatingService>();
            _mockWeatherService = new Mock<IWeatherService>();

            _mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockScope.Object);
            _mockScope.Setup(s => s.ServiceProvider).Returns(_mockProvider.Object);
            _mockProvider.Setup(p => p.GetService(typeof(IHeatingService))).Returns(_mockHeatingService.Object);
            _mockProvider.Setup(p => p.GetService(typeof(IWeatherService))).Returns(_mockWeatherService.Object);

            _action = new GlobalHoldAction(_mockConfig.Object, _mockScopeFactory.Object, _mockLogger.Object, _emailService.Object);
        }

        [Test]
        public void Name_ReturnsExpectedValue()
        {
            Assert.That(_action.Name, Is.EqualTo("Global Hold"));
        }

        [Test]
        public void Schedule_ReturnsConfigValue()
        {
            _mockConfig.Setup(c => c["HoldSchedule"]).Returns("0 12 * * *");
            Assert.That(_action.Schedule, Is.EqualTo("0 12 * * *"));
        }

        [Test]
        public async Task Action_IsColdDay_DoesNotHold()
        {
            // Arrange
            var token = new CancellationToken();
            var hours = new List<ForecastHour>();
            for (int i = 0; i < 24; i++) { hours.Add(new ForecastHour() { Temp = 1, Condition = new ForecastCondition() }); }
            var forecastDay = new ForecastDay { Hour = hours };
            var forecast = new Forecast { ForecastDay = new List<ForecastDay> { forecastDay } };

            _mockWeatherService.Setup(w => w.GetForecast(token)).ReturnsAsync(forecast);

            // Act
            await _action.Run(token);

            // Assert
            _mockWeatherService.Verify(w => w.GetForecast(token), Times.Once);
            _mockHeatingService.Verify(h => h.GlobalHold(It.IsAny<double>(), It.IsAny<int>(), token), Times.Never);
        }

        [Test]
        public async Task Action_IsSunnyDayAboveThreshold_Holds()
        {
            // Arrange
            var token = new CancellationToken();
            var hours = new List<ForecastHour>();
            for (int i = 0; i < 24; i++) { hours.Add(new ForecastHour() { Temp = 6.5, Condition = new ForecastCondition() { Text = "Sunny" } }); }
            var forecastDay = new ForecastDay { Hour = hours };
            var forecast = new Forecast { ForecastDay = new List<ForecastDay> { forecastDay } };

            _mockWeatherService.Setup(w => w.GetForecast(token)).ReturnsAsync(forecast);

            // Act
            await _action.Run(token);

            // Assert
            _mockWeatherService.Verify(w => w.GetForecast(token), Times.Once);
            _mockHeatingService.Verify(h => h.GlobalHold(It.IsAny<double>(), It.IsAny<int>(), token), Times.Once);
        }

        [Test]
        public async Task Action_IsSunnyDayBelowThreshold_DoesNotHold()
        {
            // Arrange
            var token = new CancellationToken();
            var hours = new List<ForecastHour>();
            for (int i = 0; i < 24; i++) { hours.Add(new ForecastHour() { Temp = 6.4, Condition = new ForecastCondition() { Text = "Sunny" } }); }
            var forecastDay = new ForecastDay { Hour = hours };
            var forecast = new Forecast { ForecastDay = new List<ForecastDay> { forecastDay } };

            _mockWeatherService.Setup(w => w.GetForecast(token)).ReturnsAsync(forecast);

            // Act
            await _action.Run(token);

            // Assert
            _mockWeatherService.Verify(w => w.GetForecast(token), Times.Once);
            _mockHeatingService.Verify(h => h.GlobalHold(It.IsAny<double>(), It.IsAny<int>(), token), Times.Never);
        }

        [Test]
        public async Task Action_IsWarmDayBelowThreshold_DoesNotHold()
        {
            // Arrange
            var token = new CancellationToken();
            var hours = new List<ForecastHour>();
            for (int i = 0; i < 24; i++) { hours.Add(new ForecastHour() { Temp = 10.9, Condition = new ForecastCondition() { Text = "Not Sunny" } }); }
            var forecastDay = new ForecastDay { Hour = hours };
            var forecast = new Forecast { ForecastDay = new List<ForecastDay> { forecastDay } };

            _mockWeatherService.Setup(w => w.GetForecast(token)).ReturnsAsync(forecast);

            // Act
            await _action.Run(token);

            // Assert
            _mockWeatherService.Verify(w => w.GetForecast(token), Times.Once);
            _mockHeatingService.Verify(h => h.GlobalHold(It.IsAny<double>(), It.IsAny<int>(), token), Times.Never);
        }

        [Test]
        public async Task Action_IsWarmDayAboveThreshold_Holds()
        {
            // Arrange
            var token = new CancellationToken();
            var hours = new List<ForecastHour>();
            for (int i = 0; i < 24; i++) { hours.Add(new ForecastHour() { Temp = 11, Condition = new ForecastCondition() { Text = "Not Sunny" } }); }
            var forecastDay = new ForecastDay { Hour = hours };
            var forecast = new Forecast { ForecastDay = new List<ForecastDay> { forecastDay } };

            _mockWeatherService.Setup(w => w.GetForecast(token)).ReturnsAsync(forecast);

            // Act
            await _action.Run(token);

            // Assert
            _mockWeatherService.Verify(w => w.GetForecast(token), Times.Once);
            _mockHeatingService.Verify(h => h.GlobalHold(It.IsAny<double>(), It.IsAny<int>(), token), Times.Once);
        }
    }
}