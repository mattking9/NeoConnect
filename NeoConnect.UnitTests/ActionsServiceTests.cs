using Microsoft.Extensions.Logging;
using Moq;
using NeoConnect.Services;

namespace NeoConnect.UnitTests
{
    [TestFixture]
    public class ActionsServiceTests
    {
        private Actions _actionsService;
        private Mock<IHeatingService> _mockHeatingService;
        private Mock<ILogger<Actions>> _mockLogger;
        private Mock<IWeatherService> _mockWeatherService;
        private Mock<IEmailService> _mockEmailService;
        private CancellationTokenSource _cts;

        [SetUp]
        public void Setup()
        {
            _mockHeatingService = new Mock<IHeatingService>();
            _mockLogger = new Mock<ILogger<Actions>>();
            _mockWeatherService = new Mock<IWeatherService>();
            _mockEmailService = new Mock<IEmailService>();
            _cts = new CancellationTokenSource();
            
            _actionsService = new Actions(
                _mockHeatingService.Object, 
                _mockLogger.Object, 
                _mockWeatherService.Object, 
                _mockEmailService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _cts.Dispose();
        }

        [Test]
        public async Task PerformActions_WhenNoChangesMade_DoesNotSendSummaryEmail()
        {
            // Arrange
            var forecast = new Forecast
            {
                ForecastDay = new List<ForecastDay>
                {
                    new ForecastDay()
                }
            };
            _mockWeatherService.Setup(s => s.GetForecast(It.IsAny<CancellationToken>())).ReturnsAsync(forecast);

            _mockHeatingService.Setup(s => s.GetChangesMade()).Returns(new List<string>());

            // Act
            await _actionsService.PerformActions(_cts.Token);

            // Assert
            _mockEmailService.Verify(s => s.SendSummaryEmail(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        }        

        [Test]
        public async Task PerformActions_WhenChangesMade_SendsSummaryEmail()
        {
            // Arrange
            var forecast = new Forecast
            {
                ForecastDay = new List<ForecastDay>
                {
                    new ForecastDay()
                }
            };
            _mockWeatherService.Setup(s => s.GetForecast(It.IsAny<CancellationToken>())).ReturnsAsync(forecast);

            var changes = new List<string> { "Changed recipe to x", "Set preheat duration to y minutes" };
            _mockHeatingService.Setup(s => s.GetChangesMade()).Returns(changes);

            // Act
            await _actionsService.PerformActions(_cts.Token);

            // Assert
            _mockEmailService.Verify(s => s.SendSummaryEmail(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Test]
        public async Task PerformActions_WhenForecastExceptionOccurs_SendsErrorEmail()
        {
            // Arrange
            var expectedException = new Exception("Test exception");
            _mockWeatherService.Setup(s => s.GetForecast(It.IsAny<CancellationToken>())).ThrowsAsync(expectedException);

            // Act
            await _actionsService.PerformActions(_cts.Token);

            // Assert
            _mockEmailService.Verify(s => s.SendErrorEmail(expectedException, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task PerformActions_WhenHeatingServiceExceptionOccurs_SendsErrorEmail()
        {
            // Arrange
            var forecast = new Forecast
            {
                ForecastDay = new List<ForecastDay>
                {
                    new ForecastDay()
                }
            };
            _mockWeatherService.Setup(s => s.GetForecast(It.IsAny<CancellationToken>())).ReturnsAsync(forecast);

            var expectedException = new Exception("Test exception");
            _mockHeatingService.Setup(s => s.Init(It.IsAny<CancellationToken>())).ThrowsAsync(expectedException);

            // Act
            await _actionsService.PerformActions(_cts.Token);

            // Assert
            _mockEmailService.Verify(s => s.SendErrorEmail(expectedException, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task PerformActions_WhenExceptionOccurs_CleansUp()
        {
            // Arrange
            var expectedException = new Exception("Test exception");
            _mockWeatherService.Setup(s => s.GetForecast(It.IsAny<CancellationToken>())).ThrowsAsync(expectedException);

            // Act
            await _actionsService.PerformActions(_cts.Token);

            // Assert
            _mockHeatingService.Verify(s => s.Cleanup(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task PerformActions_WhenCancellationOccurs_CleansUp()
        {
            // Arrange
            var expectedException = new OperationCanceledException("Test exception");
            _mockWeatherService.Setup(s => s.GetForecast(It.IsAny<CancellationToken>())).ThrowsAsync(expectedException);

            // Act
            await _actionsService.PerformActions(_cts.Token);

            // Assert
            _mockHeatingService.Verify(s => s.Cleanup(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task PerformActions_WhenErrorEmailFails_CleansUp()
        {
            // Arrange
            var expectedException = new Exception("Test exception");
            _mockWeatherService.Setup(s => s.GetForecast(It.IsAny<CancellationToken>())).ThrowsAsync(expectedException);
            _mockEmailService.Setup(s => s.SendErrorEmail(expectedException, It.IsAny<CancellationToken>())).ThrowsAsync(expectedException);

            // Act
            await _actionsService.PerformActions(_cts.Token);

            // Assert
            _mockHeatingService.Verify(s => s.Cleanup(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task PerformActions_WhenCompletesSuccessfully_CleansUp()
        {
            // Arrange
            // Default setup is enough

            // Act
            await _actionsService.PerformActions(_cts.Token);

            // Assert
            _mockHeatingService.Verify(s => s.Cleanup(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task PerformActions_InitializesHeatingService()
        {
            // Arrange
            // Default setup is enough

            // Act
            await _actionsService.PerformActions(_cts.Token);

            // Assert
            _mockHeatingService.Verify(s => s.Init(It.IsAny<CancellationToken>()), Times.Once);
        }        

        [Test]
        public async Task PerformActions_RunsAllHeatingServiceActionsUsingForecastForToday()
        {
            // Arrange
            var forecast = new Forecast
            {
                ForecastDay = new List<ForecastDay>
                {
                    new ForecastDay()
                }
            };
            _mockWeatherService.Setup(s => s.GetForecast(It.IsAny<CancellationToken>())).ReturnsAsync(forecast);

            // Act
            await _actionsService.PerformActions(_cts.Token);

            // Assert
            _mockHeatingService.Verify(s => s.RunRecipeBasedOnWeatherConditions(forecast.ForecastDay[0], It.IsAny<CancellationToken>()), Times.Once);
            _mockHeatingService.Verify(s => s.SetMaxPreheatDurationBasedOnWeatherConditions(forecast.ForecastDay[0], It.IsAny<CancellationToken>()), Times.Once);
        }        
    }
}