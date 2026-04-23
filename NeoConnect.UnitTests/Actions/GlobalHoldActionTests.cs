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
        public void Id_ReturnsExpectedValue()
        {
            Assert.That(_action.Id, Is.EqualTo("global_hold"));
        }

        [Test]
        public void Description_ReturnsExpectedValue()
        {
            Assert.That(_action.Description, Is.EqualTo("Holds all thermostats at 0.5° below their set temperature if it is due to be warm and/or sunny in 1 hour's time."));
        }

        [Test]
        public async Task Action_WhenForecastBelowThresholdAndNotSunny_DoesNotCallGlobalHold()
        {
            var forecast = CreateWeatherResponse(10.0, 10.5, false, false);
            _mockWeatherService.Setup(w => w.GetForecast(It.IsAny<CancellationToken>()))
                .ReturnsAsync(forecast);

            await _action.Run(CancellationToken.None);

            _mockHeatingService.Verify(h => h.GlobalHold(It.IsAny<double>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Action_WhenForecastAboveThresholdAndNotSunny_CallsGlobalHold()
        {
            var forecast = CreateWeatherResponse(12.0, 12.0, false, false);
            _mockWeatherService.Setup(w => w.GetForecast(It.IsAny<CancellationToken>()))
                .ReturnsAsync(forecast);

            await _action.Run(CancellationToken.None);

            _mockHeatingService.Verify(h => h.GlobalHold(-0.5, 1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Action_WhenForecastBelowSunnyThresholdButSunny_CallsGlobalHold()
        {
            var forecast = CreateWeatherResponse(7.0, 8.0, true, false);
            _mockWeatherService.Setup(w => w.GetForecast(It.IsAny<CancellationToken>()))
                .ReturnsAsync(forecast);

            await _action.Run(CancellationToken.None);

            _mockHeatingService.Verify(h => h.GlobalHold(-0.5, 1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Action_WhenForecastBelowSunnyThresholdAndNotSunny_DoesNotCallGlobalHold()
        {
            var forecast = CreateWeatherResponse(6.0, 6.0, false, false);
            _mockWeatherService.Setup(w => w.GetForecast(It.IsAny<CancellationToken>()))
                .ReturnsAsync(forecast);

            await _action.Run(CancellationToken.None);

            _mockHeatingService.Verify(h => h.GlobalHold(It.IsAny<double>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Action_WhenNextHourIsSunny_UsesSunnyThreshold()
        {
            var forecast = CreateWeatherResponse(7.0, 6.0, true, false);
            _mockWeatherService.Setup(w => w.GetForecast(It.IsAny<CancellationToken>()))
                .ReturnsAsync(forecast);

            await _action.Run(CancellationToken.None);

            _mockHeatingService.Verify(h => h.GlobalHold(-0.5, 1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Action_WhenNextNextHourIsSunny_UsesSunnyThreshold()
        {
            var forecast = CreateWeatherResponse(6.0, 7.0, false, true);
            _mockWeatherService.Setup(w => w.GetForecast(It.IsAny<CancellationToken>()))
                .ReturnsAsync(forecast);

            await _action.Run(CancellationToken.None);

            _mockHeatingService.Verify(h => h.GlobalHold(-0.5, 1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Action_UsesAverageOfNextTwoHours()
        {
            var forecast = CreateWeatherResponse(11.0, 12.0, false, false);
            _mockWeatherService.Setup(w => w.GetForecast(It.IsAny<CancellationToken>()))
                .ReturnsAsync(forecast);

            await _action.Run(CancellationToken.None);

            _mockHeatingService.Verify(h => h.GlobalHold(-0.5, 1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Action_PassesCancellationToken()
        {
            var cts = new CancellationTokenSource();
            var forecast = CreateWeatherResponse(15.0, 15.0, false, false);
            _mockWeatherService.Setup(w => w.GetForecast(It.IsAny<CancellationToken>()))
                .ReturnsAsync(forecast);

            await _action.Run(cts.Token);

            _mockWeatherService.Verify(w => w.GetForecast(cts.Token), Times.Once);
            _mockHeatingService.Verify(h => h.GlobalHold(-0.5, 1, cts.Token), Times.Once);
        }

        private WeatherResponse CreateWeatherResponse(double nextHourTemp, double nextNextHourTemp, bool nextHourSunny, bool nextNextHourSunny)
        {
            var currentHour = DateTime.Now.Hour;
            var nextHourIndex = currentHour < 23 ? currentHour + 1 : 23;
            var nextNextHourIndex = currentHour < 22 ? currentHour + 2 : 23;

            var hours = new List<ForecastHour>();
            for (int i = 0; i <= 23; i++)
            {
                var hour = new ForecastHour
                {
                    Temp = i == nextHourIndex ? nextHourTemp : (i == nextNextHourIndex ? nextNextHourTemp : 10.0),
                    Condition = new Condition
                    {
                        Code = (i == nextHourIndex && nextHourSunny) || (i == nextNextHourIndex && nextNextHourSunny) ? 1000 : 1006,
                        Text = (i == nextHourIndex && nextHourSunny) || (i == nextNextHourIndex && nextNextHourSunny) ? "Sunny" : "Cloudy"
                    }
                };
                hours.Add(hour);
            }

            return new WeatherResponse
            {
                Forecast = new Forecast
                {
                    ForecastDay = new List<ForecastDay>
                    {
                        new ForecastDay
                        {
                            Hour = hours
                        }
                    }
                }
            };
        }
    }
}