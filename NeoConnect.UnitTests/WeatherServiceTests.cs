using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace NeoConnect.UnitTests
{
    [TestFixture]
    public class WeatherServiceTests
    {
        private WeatherService _weatherService;
        private Mock<ILogger<WeatherService>> _mockLogger;        
        private Mock<IHttpClientFactory> _mockHttpClientFactory;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private CancellationTokenSource _cts;

        [SetUp]
        public void Setup()
        {
            // Setup mock configuration
            var inMemorySettings = new Dictionary<string, string> {
                {"WeatherApi:Uri", "http://weather.api/forecast"},
                {"WeatherApi:ApiKey", "test-api-key"},
                {"WeatherApi:Location", "London"},
                //...populate as needed for the test
            };

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _mockLogger = new Mock<ILogger<WeatherService>>();
            _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            _cts = new CancellationTokenSource();
            
            // Setup mock HttpClient
            var mockHttpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockHttpClient);

            _weatherService = new WeatherService(_mockLogger.Object, config, _mockHttpClientFactory.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _cts.Dispose();
        }

        [Test]
        public async Task GetForecast_WhenSuccessful_ReturnsForecastData()
        {
            // Arrange            
            var mockResponseContent = @"
            {
                ""forecast"": {
                    ""forecastDay"": [
                        {   
                            ""date"":""2025-09-04"", 
                            ""day"": {
                                ""avgtemp_c"":20.5
                            },
                            ""hour"": [
                                {
                                    ""time"":""00:00"",
                                    ""Temp_C"":18.5
                                },
                                {
                                    ""time"":""01:00"",
                                    ""Temp_C"":18.0
                                }
                            ]
                        }
                    ]
                }
            }";

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(mockResponseContent, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _weatherService.GetForecast(_cts.Token);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ForecastDay, Is.Not.Null);            
            Assert.That(result.ForecastDay.Count, Is.EqualTo(1));
            Assert.That(result.ForecastDay[0].Day.AverageTemp, Is.EqualTo(20.5m));
            Assert.That(result.ForecastDay[0].Hour.Count, Is.EqualTo(2));
            Assert.That(result.ForecastDay[0].Hour[0].Temp, Is.EqualTo(18.5m));
            Assert.That(result.ForecastDay[0].Hour[0].Time, Is.EqualTo("00:00"));
            Assert.That(result.ForecastDay[0].Hour[1].Temp, Is.EqualTo(18.0m));
            Assert.That(result.ForecastDay[0].Hour[1].Time, Is.EqualTo("01:00"));
        }

        [Test]
        public async Task GetForecast_WhenApiReturnsError_ThrowsException()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Invalid API key", Encoding.UTF8, "application/json")
                });

            // Act & Assert
            var ex = Assert.ThrowsAsync<HttpRequestException>(async () => await _weatherService.GetForecast(_cts.Token));
            Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task GetForecast_WhenApiReturnsUnexpectedResponse_ThrowsException()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Not a valid JSON", Encoding.UTF8, "application/json")
                });

            // Act & Assert
            Assert.ThrowsAsync<JsonException>(async () => await _weatherService.GetForecast(_cts.Token));
        }
    }
}