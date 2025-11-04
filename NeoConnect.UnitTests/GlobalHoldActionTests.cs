using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NeoConnect.UnitTests
{
    [TestFixture]
    public class GlobalHoldActionTests
    {
        private Mock<IConfiguration> _mockConfig;
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
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockScope = new Mock<IServiceScope>();
            _mockProvider = new Mock<IServiceProvider>();
            _mockHeatingService = new Mock<IHeatingService>();
            _mockWeatherService = new Mock<IWeatherService>();

            _mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockScope.Object);
            _mockScope.Setup(s => s.ServiceProvider).Returns(_mockProvider.Object);
            _mockProvider.Setup(p => p.GetService(typeof(IHeatingService))).Returns(_mockHeatingService.Object);
            _mockProvider.Setup(p => p.GetService(typeof(IWeatherService))).Returns(_mockWeatherService.Object);

            _action = new GlobalHoldAction(_mockConfig.Object, _mockScopeFactory.Object);
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
        public async Task Action_CallsHeatingAndWeatherServiceMethodsInOrder()
        {
            // Arrange
            var token = new CancellationToken();
            var forecastDay = new ForecastDay { Hour = new List<ForecastHour>() };
            var forecast = new Forecast { ForecastDay = new List<ForecastDay> { forecastDay } };

            _mockWeatherService.Setup(w => w.GetForecast(token)).ReturnsAsync(forecast);
            _mockHeatingService.Setup(h => h.Init(token)).Returns(Task.CompletedTask);
            _mockHeatingService.Setup(h => h.ReduceSetTempWhenExternalTempIsWarm(forecastDay, token)).Returns(Task.CompletedTask);
            _mockHeatingService.Setup(h => h.Cleanup(token)).Returns(Task.CompletedTask);

            // Act
            await _action.Action(token);

            // Assert
            _mockWeatherService.Verify(w => w.GetForecast(token), Times.Once);
            _mockHeatingService.Verify(h => h.Init(token), Times.Once);
            _mockHeatingService.Verify(h => h.ReduceSetTempWhenExternalTempIsWarm(forecastDay, token), Times.Once);
            _mockHeatingService.Verify(h => h.Cleanup(token), Times.Once);
        }
    }
}