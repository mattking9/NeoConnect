using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace NeoConnect.UnitTests.Services
{
    [TestFixture]
    public class SolarServiceTests
    {
        private IConfiguration _configuration;
        private Mock<ILogger<SolarService>> _mockLogger;
        private Mock<IHttpClientFactory> _mockHttpClientFactory;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private SolarService _solarService;
        private CancellationTokenSource _cts;

        private const string TestApiKey = "test_api_key_12345";
        private const string TestDeviceSerialNr = "TEST123456";
        private const string TestHost = "https://www.foxesscloud.com";

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<SolarService>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _cts = new CancellationTokenSource();

            // Use actual ConfigurationBuilder
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FoxEssCloud:ApiKey"] = TestApiKey,
                ["FoxEssCloud:DeviceSerialNr"] = TestDeviceSerialNr,
                ["FoxEssCloud:Host"] = TestHost
            });
            _configuration = configBuilder.Build();

            // Setup logger
            _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [TearDown]
        public void TearDown()
        {
            _cts?.Dispose();
        }

        #region GetRealtimeData Success Tests

        [Test]
        public async Task GetRealtimeData_WhenSuccessful_ReturnsSolarData()
        {
            // Arrange
            SetupHttpClientWithSuccessfulResponse();

            // Act
            var result = await _solarService.GetRealtimeData(_cts.Token);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.GeneratedPower, Is.EqualTo(5.2M));
            Assert.That(result.FeedInPower, Is.EqualTo(3.5M));
            Assert.That(result.SoC, Is.EqualTo(92.5M));
            Assert.That(result.Load, Is.EqualTo(1.7M));
            Assert.That(result.Timestamp, Is.EqualTo(new DateTime(2024, 4, 20, 14, 30, 45)));
        }

        [Test]
        public async Task GetRealtimeData_WhenSuccessful_SendsCorrectRequest()
        {
            // Arrange
            SetupHttpClientWithSuccessfulResponse();

            // Act
            await _solarService.GetRealtimeData(_cts.Token);

            // Assert
            VerifyHttpPostSent();
        }

        [Test]
        public async Task GetRealtimeData_WhenSuccessful_IncludesCorrectHeaders()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            SetupHttpClientWithSuccessfulResponse(request =>
            {
                capturedRequest = request;
            });

            // Act
            await _solarService.GetRealtimeData(_cts.Token);

            // Assert
            Assert.That(capturedRequest, Is.Not.Null);
            Assert.That(capturedRequest!.Content!.Headers.Contains("token"), Is.True);
            Assert.That(capturedRequest.Content.Headers.Contains("timestamp"), Is.True);
            Assert.That(capturedRequest.Content.Headers.Contains("signature"), Is.True);
            Assert.That(capturedRequest.Content.Headers.Contains("lang"), Is.True);

            var tokenValue = capturedRequest.Content.Headers.GetValues("token").FirstOrDefault();
            Assert.That(tokenValue, Is.EqualTo(TestApiKey));
        }

        [Test]
        public async Task GetRealtimeData_WhenSuccessful_IncludesCorrectRequestBody()
        {
            // Arrange
            string? capturedBody = null;
            SetupHttpClientWithSuccessfulResponse(async request =>
            {
                capturedBody = await request.Content!.ReadAsStringAsync();
            });

            // Act
            await _solarService.GetRealtimeData(_cts.Token);

            // Assert
            Assert.That(capturedBody, Is.Not.Null);
            Assert.That(capturedBody, Does.Contain(TestDeviceSerialNr));
            Assert.That(capturedBody, Does.Contain("generationPower"));
            Assert.That(capturedBody, Does.Contain("feedinPower"));
            Assert.That(capturedBody, Does.Contain("SoC"));
            Assert.That(capturedBody, Does.Contain("loadsPower"));
        }

        [Test]
        public async Task GetRealtimeData_WhenSuccessful_LogsDebugMessages()
        {
            // Arrange
            SetupHttpClientWithSuccessfulResponse();

            // Act
            await _solarService.GetRealtimeData(_cts.Token);

            // Assert
            VerifyLogMessage(LogLevel.Debug, "Posting request to foxesscloud");
            VerifyLogMessage(LogLevel.Debug, "FoxESS Response:");
        }

        #endregion

        #region GetRealtimeData Error Tests

        [Test]
        public void GetRealtimeData_WhenHttpRequestFails_ThrowsException()
        {
            // Arrange
            SetupHttpClientWithFailure(HttpStatusCode.InternalServerError);

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(
                async () => await _solarService.GetRealtimeData(_cts.Token));

            Assert.That(ex!.Message, Does.Contain("Request failed with status code"));
            Assert.That(ex.Message, Does.Contain("InternalServerError"));
        }

        [Test]
        public void GetRealtimeData_WhenApiReturnsError_ThrowsHttpRequestException()
        {
            // Arrange
            var errorResponse = new FoxEssApiResponse
            {
                Errno = 41808,
                Msg = "Invalid token",
                Result = new List<FoxEssApiResponse.ResultType>()
            };

            SetupHttpClientWithApiError(errorResponse);

            // Act & Assert
            var ex = Assert.ThrowsAsync<HttpRequestException>(
                async () => await _solarService.GetRealtimeData(_cts.Token));

            Assert.That(ex!.Message, Does.Contain("Error calling FoxESS Cloud API"));
            Assert.That(ex.Message, Does.Contain("41808"));
            Assert.That(ex.Message, Does.Contain("Invalid token"));
        }

        [Test]
        public void GetRealtimeData_WhenResponseCannotBeDeserialized_ThrowsException()
        {
            // Arrange
            SetupHttpClientWithInvalidJson();

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(
                async () => await _solarService.GetRealtimeData(_cts.Token));

            Assert.That(ex!.Message, Does.Contain("Unable to succesfully deserialize"));
        }

        [Test]
        public void GetRealtimeData_WhenCancellationRequested_ThrowsOperationCanceledException()
        {
            // Arrange
            SetupHttpClientWithCancellation();
            _cts.Cancel();

            // Act & Assert
            Assert.ThrowsAsync<TaskCanceledException>(
                async () => await _solarService.GetRealtimeData(_cts.Token));
        }

        [Test]
        public void GetRealtimeData_WhenNetworkError_ThrowsException()
        {
            // Arrange
            SetupHttpClientWithNetworkError();

            // Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(
                async () => await _solarService.GetRealtimeData(_cts.Token));
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public async Task GetRealtimeData_WithDifferentVariableOrdering_ReturnsCorrectData()
        {
            // Arrange - FoxESS might return variables in different order
            var apiResponse = new FoxEssApiResponse
            {
                Errno = 0,
                Msg = "success",
                Result = new List<FoxEssApiResponse.ResultType>
                {
                    new FoxEssApiResponse.ResultType
                    {
                        DeviceSN = TestDeviceSerialNr,
                        Time = "2024-04-20 14:30:45 GMT",
                        Datas = new List<FoxEssApiResponse.ResultType.DataType>
                        {
                            new() { Variable = "SoC", Value = 85.0M, Unit = "%", Name = "State of Charge" },
                            new() { Variable = "loadsPower", Value = 2.0M, Unit = "kW", Name = "Load Power" },
                            new() { Variable = "feedinPower", Value = 1.5M, Unit = "kW", Name = "Feed-in Power" },
                            new() { Variable = "generationPower", Value = 3.5M, Unit = "kW", Name = "Generation Power" }
                        }
                    }
                }
            };

            SetupHttpClientWithCustomResponse(apiResponse);

            // Act
            var result = await _solarService.GetRealtimeData(_cts.Token);

            // Assert
            Assert.That(result.GeneratedPower, Is.EqualTo(3.5M));
            Assert.That(result.FeedInPower, Is.EqualTo(1.5M));
            Assert.That(result.SoC, Is.EqualTo(85.0M));
            Assert.That(result.Load, Is.EqualTo(2.0M));
        }

        [Test]
        public async Task GetRealtimeData_WithZeroValues_ReturnsCorrectData()
        {
            // Arrange - e.g., at night when no solar generation
            var apiResponse = new FoxEssApiResponse
            {
                Errno = 0,
                Msg = "success",
                Result = new List<FoxEssApiResponse.ResultType>
                {
                    new FoxEssApiResponse.ResultType
                    {
                        DeviceSN = TestDeviceSerialNr,
                        Time = "2024-04-20 22:00:00 GMT",
                        Datas = new List<FoxEssApiResponse.ResultType.DataType>
                        {
                            new() { Variable = "generationPower", Value = 0M, Unit = "kW", Name = "Generation Power" },
                            new() { Variable = "feedinPower", Value = 0M, Unit = "kW", Name = "Feed-in Power" },
                            new() { Variable = "SoC", Value = 45.0M, Unit = "%", Name = "State of Charge" },
                            new() { Variable = "loadsPower", Value = 0.5M, Unit = "kW", Name = "Load Power" }
                        }
                    }
                }
            };

            SetupHttpClientWithCustomResponse(apiResponse);

            // Act
            var result = await _solarService.GetRealtimeData(_cts.Token);

            // Assert
            Assert.That(result.GeneratedPower, Is.EqualTo(0M));
            Assert.That(result.FeedInPower, Is.EqualTo(0M));
            Assert.That(result.SoC, Is.EqualTo(45.0M));
            Assert.That(result.Load, Is.EqualTo(0.5M));
        }

        [Test]
        public async Task GetRealtimeData_ParsesTimestampCorrectly()
        {
            // Arrange
            var apiResponse = new FoxEssApiResponse
            {
                Errno = 0,
                Msg = "success",
                Result = new List<FoxEssApiResponse.ResultType>
                {
                    new FoxEssApiResponse.ResultType
                    {
                        DeviceSN = TestDeviceSerialNr,
                        Time = "2024-12-25 09:15:30 GMT",
                        Datas = new List<FoxEssApiResponse.ResultType.DataType>
                        {
                            new() { Variable = "generationPower", Value = 1.0M, Unit = "kW", Name = "Generation Power" },
                            new() { Variable = "feedinPower", Value = 0.5M, Unit = "kW", Name = "Feed-in Power" },
                            new() { Variable = "SoC", Value = 50.0M, Unit = "%", Name = "State of Charge" },
                            new() { Variable = "loadsPower", Value = 0.5M, Unit = "kW", Name = "Load Power" }
                        }
                    }
                }
            };

            SetupHttpClientWithCustomResponse(apiResponse);

            // Act
            var result = await _solarService.GetRealtimeData(_cts.Token);

            // Assert
            Assert.That(result.Timestamp.Year, Is.EqualTo(2024));
            Assert.That(result.Timestamp.Month, Is.EqualTo(12));
            Assert.That(result.Timestamp.Day, Is.EqualTo(25));
            Assert.That(result.Timestamp.Hour, Is.EqualTo(9));
            Assert.That(result.Timestamp.Minute, Is.EqualTo(15));
            Assert.That(result.Timestamp.Second, Is.EqualTo(30));
        }

        #endregion

        #region Helper Methods

        private void SetupHttpClientWithSuccessfulResponse(Action<HttpRequestMessage>? requestCallback = null)
        {
            var apiResponse = new FoxEssApiResponse
            {
                Errno = 0,
                Msg = "success",
                Result = new List<FoxEssApiResponse.ResultType>
                {
                    new FoxEssApiResponse.ResultType
                    {
                        DeviceSN = TestDeviceSerialNr,
                        Time = "2024-04-20 14:30:45 GMT",
                        Datas = new List<FoxEssApiResponse.ResultType.DataType>
                        {
                            new() { Variable = "generationPower", Value = 5.2M, Unit = "kW", Name = "Generation Power" },
                            new() { Variable = "feedinPower", Value = 3.5M, Unit = "kW", Name = "Feed-in Power" },
                            new() { Variable = "SoC", Value = 92.5M, Unit = "%", Name = "State of Charge" },
                            new() { Variable = "loadsPower", Value = 1.7M, Unit = "kW", Name = "Load Power" }
                        }
                    }
                }
            };

            SetupHttpClientWithCustomResponse(apiResponse, requestCallback);
        }

        private void SetupHttpClientWithCustomResponse(FoxEssApiResponse apiResponse, Action<HttpRequestMessage>? requestCallback = null)
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    requestCallback?.Invoke(request);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(apiResponse))
                    };
                });

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _solarService = new SolarService(_configuration, _mockLogger.Object, _mockHttpClientFactory.Object);
        }

        private void SetupHttpClientWithFailure(HttpStatusCode statusCode)
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent("")
                });

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _solarService = new SolarService(_configuration, _mockLogger.Object, _mockHttpClientFactory.Object);
        }

        private void SetupHttpClientWithApiError(FoxEssApiResponse errorResponse)
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(errorResponse))
                });

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _solarService = new SolarService(_configuration, _mockLogger.Object, _mockHttpClientFactory.Object);
        }

        private void SetupHttpClientWithInvalidJson()
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null")
                });

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _solarService = new SolarService(_configuration, _mockLogger.Object, _mockHttpClientFactory.Object);
        }

        private void SetupHttpClientWithCancellation()
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException());

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _solarService = new SolarService(_configuration, _mockLogger.Object, _mockHttpClientFactory.Object);
        }

        private void SetupHttpClientWithNetworkError()
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _solarService = new SolarService(_configuration, _mockLogger.Object, _mockHttpClientFactory.Object);
        }

        private void VerifyHttpPostSent()
        {
            _mockHttpMessageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.AbsolutePath == "/op/v1/device/real/query"),
                    ItExpr.IsAny<CancellationToken>());
        }

        private void VerifyLogMessage(LogLevel logLevel, string message)
        {
            _mockLogger.Verify(
                x => x.Log(
                    logLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        #endregion
    }
}