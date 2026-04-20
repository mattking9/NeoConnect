using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace NeoConnect.UnitTests
{
    [TestFixture]
    public class RunImmersionActionTests
    {
        private RunImmersionAction _action;
        private Mock<IEmailService> _mockEmailService;
        private Mock<IServiceScopeFactory> _mockServiceScopeFactory;
        private Mock<IServiceScope> _mockServiceScope;
        private Mock<IServiceProvider> _mockServiceProvider;
        private Mock<ISolarService> _mockSolarService;
        private Mock<IImmersionService> _mockImmersionService;
        private Mock<IHeatingService> _mockHeatingService;
        private Mock<ILogger<RunImmersionAction>> _mockLogger;
        private IConfiguration _configuration;
        private CancellationTokenSource _cts;

        [SetUp]
        public void Setup()
        {
            _mockEmailService = new Mock<IEmailService>();
            _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            _mockServiceScope = new Mock<IServiceScope>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockSolarService = new Mock<ISolarService>();
            _mockImmersionService = new Mock<IImmersionService>();
            _mockHeatingService = new Mock<IHeatingService>();
            _mockLogger = new Mock<ILogger<RunImmersionAction>>();
            _cts = new CancellationTokenSource();

            // Setup configuration
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RunImmersionSchedule"] = "0 */5 * * *"
            });
            _configuration = configBuilder.Build();

            // Setup logger
            _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            // Setup service scope factory
            _mockServiceScopeFactory.Setup(f => f.CreateScope()).Returns(_mockServiceScope.Object);
            _mockServiceScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);

            // Setup service provider to return mocked services
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(ISolarService))).Returns(_mockSolarService.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IImmersionService))).Returns(_mockImmersionService.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IHeatingService))).Returns(_mockHeatingService.Object);

            _action = new RunImmersionAction(
                _configuration,
                _mockEmailService.Object,
                _mockServiceScopeFactory.Object,
                _mockLogger.Object);

            _action.TestMode = true;
        }

        [TearDown]
        public void TearDown()
        {
            _cts?.Dispose();
        }

        #region Property Tests

        [Test]
        public void Id_ReturnsCorrectValue()
        {
            Assert.That(_action.Id, Is.EqualTo("run_immersion"));
        }

        [Test]
        public void Name_ReturnsCorrectValue()
        {
            Assert.That(_action.Name, Is.EqualTo("Run Immersion"));
        }

        [Test]
        public void Description_ReturnsCorrectValue()
        {
            Assert.That(_action.Description, Does.Contain("Turns on the immersion"));
        }

        [Test]
        public void Schedule_ReturnsConfiguredValue()
        {
            Assert.That(_action.Schedule, Is.EqualTo("0 */5 * * *"));
        }

        #endregion

        #region Stable Export Tests

        [Test]
        public async Task ExecuteAsync_WhenSolarExportIsStable_TurnsOnImmersion()
        {
            // Arrange
            var solarData = CreateStableSolarData();
            _mockSolarService.Setup(s => s.GetRealtimeData(It.IsAny<CancellationToken>()))
                .ReturnsAsync(solarData);

            // Cancel after immersion is turned on to exit the monitoring loop
            _mockImmersionService.Setup(s => s.TurnOnDevice(It.IsAny<CancellationToken>()))
                .Callback(() => _cts.Cancel());

            // Act
            await _action.Run(_cts.Token);

            // Assert
            _mockSolarService.Verify(s => s.GetRealtimeData(It.IsAny<CancellationToken>()), Times.AtLeast(3));
            _mockImmersionService.Verify(s => s.TurnOnDevice(It.IsAny<CancellationToken>()), Times.Once);
            _mockEmailService.Verify(e => e.SendInfoEmail("Immersion was turned ON.", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_WhenSolarExportBelowThreshold_DoesNotTurnOnImmersion()
        {
            // Arrange
            var solarData = new SolarData
            {
                FeedInPower = 2.0M, // Below threshold of 3.2
                SoC = 95M,
                Load = 1.0M,
                GeneratedPower = 5.0M,
                Timestamp = DateTime.Now
            };
            _mockSolarService.Setup(s => s.GetRealtimeData(It.IsAny<CancellationToken>()))
                .ReturnsAsync(solarData);

            // Act
            await _action.Run(_cts.Token);

            // Assert
            _mockImmersionService.Verify(s => s.TurnOnDevice(It.IsAny<CancellationToken>()), Times.Never);
            _mockEmailService.Verify(e => e.SendInfoEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_WhenBatteryBelowThreshold_DoesNotTurnOnImmersion()
        {
            // Arrange
            var solarData = new SolarData
            {
                FeedInPower = 4.0M,
                SoC = 85M, // Below threshold of 90%
                Load = 1.0M,
                GeneratedPower = 5.0M,
                Timestamp = DateTime.Now
            };
            _mockSolarService.Setup(s => s.GetRealtimeData(It.IsAny<CancellationToken>()))
                .ReturnsAsync(solarData);

            // Act
            await _action.Run(_cts.Token);

            // Assert
            _mockImmersionService.Verify(s => s.TurnOnDevice(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_WhenSolarDataIsStale_DoesNotTurnOnImmersion()
        {
            // Arrange
            var solarData = new SolarData
            {
                FeedInPower = 4.0M,
                SoC = 95M,
                Load = 1.0M,
                GeneratedPower = 5.0M,
                Timestamp = DateTime.Now.AddMinutes(-10) // Stale data
            };
            _mockSolarService.Setup(s => s.GetRealtimeData(It.IsAny<CancellationToken>()))
                .ReturnsAsync(solarData);

            // Act
            await _action.Run(_cts.Token);

            // Assert
            _mockImmersionService.Verify(s => s.TurnOnDevice(It.IsAny<CancellationToken>()), Times.Never);
            VerifyLogMessage(LogLevel.Warning, "Failed to retrieve up-to-date solar data");
        }

        #endregion

        #region Monitoring Loop Tests

        [Test]
        public async Task ExecuteAsync_WhenHeatingComplete_TurnsOffImmersionAndBoilerHotWater()
        {
            // Arrange
            var setupCalls = 0;
            _mockSolarService.Setup(s => s.GetRealtimeData(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    setupCalls++;
                    if (setupCalls <= 3)
                    {
                        // Initial stable readings
                        return CreateStableSolarData();
                    }
                    else
                    {
                        // Heating complete - load drops below 2.8kW
                        return new SolarData
                        {
                            FeedInPower = 4.0M,
                            SoC = 95M,
                            Load = 2.5M, // Below 2.8kW threshold
                            GeneratedPower = 5.0M,
                            Timestamp = DateTime.Now
                        };
                    }
                });

            // Act
            await _action.Run(_cts.Token);

            // Assert
            _mockImmersionService.Verify(s => s.TurnOnDevice(It.IsAny<CancellationToken>()), Times.Once);
            _mockImmersionService.Verify(s => s.TurnOffDevice(It.IsAny<CancellationToken>()), Times.Once);
            _mockHeatingService.Verify(h => h.TurnOffHotWater(8, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_WhenSolarGenerationInsufficient_TurnsOffImmersion()
        {
            // Arrange
            var setupCalls = 0;
            _mockSolarService.Setup(s => s.GetRealtimeData(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    setupCalls++;
                    if (setupCalls <= 3)
                    {
                        // Initial stable readings
                        return CreateStableSolarData();
                    }
                    else
                    {
                        // Solar generating less than load with battery below 95%
                        return new SolarData
                        {
                            FeedInPower = 0M,
                            SoC = 94M,
                            Load = 5.0M,
                            GeneratedPower = 3.0M, // Less than load
                            Timestamp = DateTime.Now
                        };
                    }
                });

            // Act
            await _action.Run(_cts.Token);

            // Assert
            _mockImmersionService.Verify(s => s.TurnOnDevice(It.IsAny<CancellationToken>()), Times.Once);
            _mockImmersionService.Verify(s => s.TurnOffDevice(It.IsAny<CancellationToken>()), Times.Once);
            _mockEmailService.Verify(e => e.SendInfoEmail(
                It.Is<string>(msg => msg.Contains("aborted before target temperature")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_WhenDataBecomesStale_TurnsOffImmersion()
        {
            // Arrange
            var setupCalls = 0;
            _mockSolarService.Setup(s => s.GetRealtimeData(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    setupCalls++;
                    if (setupCalls <= 3)
                    {
                        return CreateStableSolarData();
                    }
                    else
                    {
                        // Return stale data
                        return new SolarData
                        {
                            FeedInPower = 4.0M,
                            SoC = 95M,
                            Load = 3.0M,
                            GeneratedPower = 5.0M,
                            Timestamp = DateTime.Now.AddMinutes(-10) // Stale
                        };
                    }
                });

            // Act
            await _action.Run(_cts.Token);

            // Assert
            _mockImmersionService.Verify(s => s.TurnOffDevice(It.IsAny<CancellationToken>()), Times.Once);
            _mockEmailService.Verify(e => e.SendInfoEmail(
                It.Is<string>(msg => msg.Contains("Unable to fetch recent data")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Exception Handling Tests

        [Test]
        public async Task ExecuteAsync_WhenExceptionOccurs_TurnsOffImmersionAndRethrows()
        {
            // Arrange
            var solarData = CreateStableSolarData();
            var callCount = 0;
            _mockSolarService.Setup(s => s.GetRealtimeData(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    if (callCount <= 3)
                    {
                        return solarData;
                    }
                    throw new InvalidOperationException("Test exception");
                });

            // Act & Assert
            await _action.Run(_cts.Token);

            // Verify immersion was turned off
            _mockImmersionService.Verify(s => s.TurnOffDevice(It.IsAny<CancellationToken>()), Times.Once);
            VerifyLogMessage(LogLevel.Warning, "Immersion was turned OFF after exception was thrown");

            // Verify error email was sent (from base class)
            _mockEmailService.Verify(e => e.SendErrorEmail(
                It.IsAny<Exception>(), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_WhenCancellationRequested_DoesNotTurnOffImmersionIfNotOn()
        {
            // Arrange
            var solarData = new SolarData
            {
                FeedInPower = 2.0M, // Below threshold
                SoC = 95M,
                Load = 1.0M,
                GeneratedPower = 5.0M,
                Timestamp = DateTime.Now
            };
            _mockSolarService.Setup(s => s.GetRealtimeData(It.IsAny<CancellationToken>()))
                .ReturnsAsync(solarData);

            _cts.Cancel();

            // Act
            await _action.Run(_cts.Token);

            // Assert
            _mockImmersionService.Verify(s => s.TurnOffDevice(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Concurrent Execution Tests

        [Test]
        public async Task Run_WhenAlreadyRunning_DoesNotExecuteAgain()
        {
            // Arrange
            var solarData = CreateStableSolarData();
            var firstCallStarted = new TaskCompletionSource<bool>();
            var continueFirstCall = new TaskCompletionSource<bool>();

            _mockSolarService.Setup(s => s.GetRealtimeData(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    firstCallStarted.TrySetResult(true);
                    continueFirstCall.Task.Wait();
                    return solarData;
                });

            // Act
            var firstTask = Task.Run(async () => await _action.Run(_cts.Token));

            // Wait for first execution to start
            await firstCallStarted.Task;

            // Try to run again while first is still running
            await _action.Run(_cts.Token);

            // Complete first execution
            continueFirstCall.SetResult(true);
            _cts.Cancel();
            await firstTask;

            // Assert
            VerifyLogMessage(LogLevel.Warning, "is already running");
            // Solar service should only be called from first execution
            _mockSolarService.Verify(s => s.GetRealtimeData(It.IsAny<CancellationToken>()), Times.AtLeast(1));
        }

        #endregion

        #region Helper Methods

        private SolarData CreateStableSolarData()
        {
            return new SolarData
            {
                FeedInPower = 4.0M, // Above 3.2 threshold
                SoC = 95M, // Above 90% threshold
                Load = 3.0M,
                GeneratedPower = 7.0M,
                Timestamp = DateTime.Now
            };
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