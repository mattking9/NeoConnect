using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace NeoConnect.UnitTests.Services
{
    [TestFixture]
    public class ImmersionServiceTests
    {
        private IConfiguration _configuration;
        private Mock<ILogger<ImmersionService>> _mockLogger;
        private Mock<IHttpClientFactory> _mockHttpClientFactory;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private ImmersionService _immersionService;
        private CancellationTokenSource _cts;

        private const string TestAccessId = "test_access_id";
        private const string TestAccessSecret = "test_access_secret";
        private const string TestDeviceId = "test_device_id";
        private const string TestTuyaHost = "https://openapi.tuyaus.com";

        [SetUp]
        public void Setup()
        {
            // Use actual ConfigurationBuilder instead of mocking
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tuya:AccessId"] = TestAccessId,
                ["Tuya:AccessSecret"] = TestAccessSecret,
                ["Tuya:DeviceId"] = TestDeviceId,
                ["Tuya:Host"] = TestTuyaHost
            });
            _configuration = configBuilder.Build();

            _mockLogger = new Mock<ILogger<ImmersionService>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _cts = new CancellationTokenSource();

            // Setup logger
            _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [TearDown]
        public void TearDown()
        {
            _cts?.Dispose();
        }

        [Test]
        public async Task TurnOnDevice_WhenSuccessful_LogsInformationAndSendsCorrectRequest()
        {
            // Arrange
            SetupHttpClientWithResponses(createSuccessfulResponses: true);

            // Act
            await _immersionService.TurnOnDevice(_cts.Token);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Turning ON Immersion")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);

            VerifyHttpCallsMade(2); // 1 for token, 1 for command
        }

        [Test]
        public async Task TurnOffDevice_WhenSuccessful_LogsInformationAndSendsCorrectRequest()
        {
            // Arrange
            SetupHttpClientWithResponses(createSuccessfulResponses: true);

            // Act
            await _immersionService.TurnOffDevice(_cts.Token);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Turning OFF Immersion")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);

            VerifyHttpCallsMade(2); // 1 for token, 1 for command
        }

        [Test]
        public void TurnOnDevice_WhenTuyaApiReturnsFailure_ThrowsHttpRequestException()
        {
            // Arrange
            SetupHttpClientWithResponses(createSuccessfulResponses: false, commandSuccess: false);

            // Act & Assert
            var ex = Assert.ThrowsAsync<HttpRequestException>(
                async () => await _immersionService.TurnOnDevice(_cts.Token));
            
            Assert.That(ex!.Message, Does.Contain("Failed to turn ON Immersion switch"));
        }

        [Test]
        public void TurnOffDevice_WhenTuyaApiReturnsFailure_ThrowsHttpRequestException()
        {
            // Arrange
            SetupHttpClientWithResponses(createSuccessfulResponses: false, commandSuccess: false);

            // Act & Assert
            var ex = Assert.ThrowsAsync<HttpRequestException>(
                async () => await _immersionService.TurnOffDevice(_cts.Token));
            
            Assert.That(ex!.Message, Does.Contain("Failed to turn OFF Immersion switch"));
        }

        [Test]
        public void TurnOnDevice_WhenHttpRequestFails_ThrowsException()
        {
            // Arrange
            SetupHttpClientWithResponses(createSuccessfulResponses: true, commandStatusCode: HttpStatusCode.InternalServerError);

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(
                async () => await _immersionService.TurnOnDevice(_cts.Token));
            
            Assert.That(ex!.Message, Does.Contain("Tuya API request failed"));
        }

        [Test]
        public void TurnOffDevice_WhenHttpRequestFails_ThrowsException()
        {
            // Arrange
            SetupHttpClientWithResponses(createSuccessfulResponses: true, commandStatusCode: HttpStatusCode.BadRequest);

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(
                async () => await _immersionService.TurnOffDevice(_cts.Token));
            
            Assert.That(ex!.Message, Does.Contain("Tuya API request failed"));
        }

        [Test]
        public void TurnOnDevice_WhenCancellationRequested_ThrowsOperationCanceledException()
        {
            // Arrange
            SetupHttpClientWithCancellation();
            _cts.Cancel();

            // Act & Assert
            Assert.ThrowsAsync<TaskCanceledException>(
                async () => await _immersionService.TurnOnDevice(_cts.Token));
        }

        [Test]
        public void TurnOffDevice_WhenCancellationRequested_ThrowsOperationCanceledException()
        {
            // Arrange
            SetupHttpClientWithCancellation();
            _cts.Cancel();

            // Act & Assert
            Assert.ThrowsAsync<TaskCanceledException>(
                async () => await _immersionService.TurnOffDevice(_cts.Token));
        }

        [Test]
        public void TurnOnDevice_WhenResponseCannotBeDeserialized_ThrowsException()
        {
            // Arrange
            SetupHttpClientWithInvalidResponse();

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(
                async () => await _immersionService.TurnOnDevice(_cts.Token));
            
            Assert.That(ex!.Message, Does.Contain("Unable to succesfully deserialize"));
        }

        #region Helper Methods

        private void SetupHttpClientWithResponses(
            bool createSuccessfulResponses = true,
            bool commandSuccess = true,
            HttpStatusCode commandStatusCode = HttpStatusCode.OK)
        {
            var tokenResponse = new
            {
                result = new
                {
                    access_token = "test_access_token"
                },
                success = true
            };

            var commandResponse = new TuyaApiResponse
            {
                Success = commandSuccess,
                Result = commandSuccess,
                Msg = commandSuccess ? "Success" : "Device not found"
            };

            var callCount = 0;
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        // First call: return token
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
                        };
                    }
                    else
                    {
                        // Second call: return command response
                        if (commandStatusCode == HttpStatusCode.OK)
                        {
                            return new HttpResponseMessage(HttpStatusCode.OK)
                            {
                                Content = new StringContent(JsonSerializer.Serialize(commandResponse))
                            };
                        }
                        else
                        {
                            return new HttpResponseMessage(commandStatusCode)
                            {
                                Content = new StringContent("")
                            };
                        }
                    }
                });

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(TestTuyaHost)
            };
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _immersionService = new ImmersionService(
                _configuration,
                _mockLogger.Object,
                _mockHttpClientFactory.Object);
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

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(TestTuyaHost)
            };
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _immersionService = new ImmersionService(
                _configuration,
                _mockLogger.Object,
                _mockHttpClientFactory.Object);
        }

        private void SetupHttpClientWithInvalidResponse()
        {
            var tokenResponse = new
            {
                result = new
                {
                    access_token = "test_access_token"
                },
                success = true
            };

            var callCount = 0;
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        // First call: return token
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
                        };
                    }
                    else
                    {
                        // Second call: return null (simulates deserialization failure)
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent("null")
                        };
                    }
                });

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(TestTuyaHost)
            };
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _immersionService = new ImmersionService(
                _configuration,
                _mockLogger.Object,
                _mockHttpClientFactory.Object);
        }

        private void VerifyHttpCallsMade(int expectedCalls)
        {
            _mockHttpMessageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Exactly(expectedCalls),
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
        }

        #endregion
    }
}