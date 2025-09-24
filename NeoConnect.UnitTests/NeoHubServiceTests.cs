using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace NeoConnect.UnitTests
{
    [TestFixture]
    public class NeoHubServiceTests
    {
        private Mock<ILogger<NeoHubService>> _mockLogger;
        private IConfiguration _configuration;
        private ProfileSchedule schedule;

        [SetUp]
        public void Setup()
        {
            // Setup mock configuration
            var inMemorySettings = new Dictionary<string, string> {
                {"NeoHub:Uri", "wss://192.168.1.1:1234"},
                {"NeoHub:ApiKey", "test-api-key"},                                
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _mockLogger = new Mock<ILogger<NeoHubService>>();
            _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            // Setup a sample schedule
            schedule = new ProfileSchedule
            {
                Weekdays = new ProfileScheduleGroup
                {
                    Wake = new object[] { "07:00", "20.5" },
                    Leave = new object[] { "08:00", "20.4" },                                        
                    Return = new object[] { "17:00", "20.3" },
                    Sleep = new object[] { "22:00", "20.2" },
                },
                Weekends = new ProfileScheduleGroup
                {
                    Wake = new object[] { "07:30", "20.9" },
                    Leave = new object[] { "08:30", "20.8" },
                    Return = new object[] { "17:30", "20.7" },
                    Sleep = new object[] { "22:30", "20.6" },
                },
            };
        }

        [Test]
        public async Task Connect_SuccessfulConnection_CompletesSuccessfully()
        {
            //arrange
            var websocketWrapperMock = new Mock<ClientWebSocketWrapper>();            

            //act
            NeoHubService service = new NeoHubService(_mockLogger.Object, _configuration, websocketWrapperMock.Object);
            await service.Connect(CancellationToken.None);

            //assert
            websocketWrapperMock.Verify(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Connect_ErrorDuringConnection_ThrowsException()
        {
            //arrange
            var websocketWrapperMock = new Mock<ClientWebSocketWrapper>();
            websocketWrapperMock.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Connection failed"));

            //act and assert
            Assert.ThrowsAsync<Exception>(async () =>
            {
                NeoHubService service = new NeoHubService(_mockLogger.Object, _configuration, websocketWrapperMock.Object);
                await service.Connect(CancellationToken.None);
            });
        }

        [Test]
        public async Task Disconnect_WebSocketNotConnected_DoesNotClose()
        {
            //arrange
            var websocketWrapperMock = new Mock<ClientWebSocketWrapper>();              

            //act
            NeoHubService service = new NeoHubService(_mockLogger.Object, _configuration, websocketWrapperMock.Object);
            await service.Disconnect(CancellationToken.None);

            //assert
            websocketWrapperMock.Verify(w => w.CloseAsync(It.IsAny<CancellationToken>()), Times.Never);
            websocketWrapperMock.Verify(w => w.Dispose(), Times.Once);
        }

        [Test]
        public async Task Disconnect_WebSocketAlreadyClosed_DoesNotClose()
        {
            //arrange
            var websocketWrapperMock = new Mock<ClientWebSocketWrapper>();
            websocketWrapperMock.Setup(w => w.State).Returns(System.Net.WebSockets.WebSocketState.Closed);

            //act
            NeoHubService service = new NeoHubService(_mockLogger.Object, _configuration, websocketWrapperMock.Object);
            await service.Disconnect(CancellationToken.None);

            //assert
            websocketWrapperMock.Verify(w => w.CloseAsync(It.IsAny<CancellationToken>()), Times.Never);
            websocketWrapperMock.Verify(w => w.Dispose(), Times.Once);
        }

        [Test]
        public async Task Disconnect_WebSocketConnected_ClosesConnection()
        {
            //arrange
            var websocketWrapperMock = new Mock<ClientWebSocketWrapper>();
            websocketWrapperMock.Setup(w => w.State).Returns(System.Net.WebSockets.WebSocketState.Open);

            //act
            NeoHubService service = new NeoHubService(_mockLogger.Object, _configuration, websocketWrapperMock.Object);
            await service.Disconnect(CancellationToken.None);

            //assert
            websocketWrapperMock.Verify(w => w.CloseAsync(It.IsAny<CancellationToken>()), Times.Once);
            websocketWrapperMock.Verify(w => w.Dispose(), Times.Once);
        }

        [Test]
        public async Task GetDevices_WebSocketNotConnected_ThrowsException()
        {
            //arrange
            var websocketWrapperMock = new Mock<ClientWebSocketWrapper>();
            websocketWrapperMock.Setup(w => w.State).Returns(System.Net.WebSockets.WebSocketState.Closed);
            //act and assert
            NeoHubService service = new NeoHubService(_mockLogger.Object, _configuration, websocketWrapperMock.Object);            
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.GetDevices(CancellationToken.None);
            });
        }

        [Test]
        public async Task GetDevices_WebSocketConnected_ValidRequest_ReturnsDevices()
        {
            //arrange
            var websocketWrapperMock = new Mock<ClientWebSocketWrapper>();
            websocketWrapperMock.Setup(w => w.State).Returns(System.Net.WebSockets.WebSocketState.Open);

            var devices = new List<NeoDevice>
            {
                new NeoDevice { ZoneName = "Living Room", IsThermostat = true, IsOffline = false, ActiveProfile = 1, ActualTemp = "18.9" },
                new NeoDevice { ZoneName = "Bedroom", IsThermostat = true, IsOffline = false, ActiveProfile = 1, ActualTemp = "19.5" }
            };
            var neoHubResponse = new NeoHubResponse
            {
                CommandId = 1,
                DeviceId = "0",
                MessageType = "RESPONSE",
                ResponseJson = JsonSerializer.Serialize(new NeoHubLiveData { Devices = devices })
            };
            websocketWrapperMock.Setup(w => w.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(JsonSerializer.Serialize(neoHubResponse));

            //act
            NeoHubService service = new NeoHubService(_mockLogger.Object, _configuration, websocketWrapperMock.Object);
            var result = await service.GetDevices(CancellationToken.None);

            //assert
            Assert.That(result.Count == 2);
            Assert.That(result[0].ZoneName == "Living Room");
            Assert.That(result[0].ActualTemp == "18.9");
            Assert.That(result[1].ZoneName == "Bedroom");
            Assert.That(result[1].ActualTemp == "19.5");

        }        

        [TestCase("2025-07-21 00:00:00", "07:00", 20.5)] // Monday
        [TestCase("2025-07-21 06:59:59", "07:00", 20.5)]
        [TestCase("2025-07-21 07:00:00", "08:00", 20.4)]
        [TestCase("2025-07-21 07:59:59", "08:00", 20.4)]
        [TestCase("2025-07-21 08:00:00", "17:00", 20.3)]
        [TestCase("2025-07-21 16:59:59", "17:00", 20.3)]
        [TestCase("2025-07-21 17:00:00", "22:00", 20.2)]
        [TestCase("2025-07-21 21:59:59", "22:00", 20.2)]
        [TestCase("2025-07-25 00:00:00", "07:00", 20.5)] // Friday
        [TestCase("2025-07-25 06:59:59", "07:00", 20.5)]
        [TestCase("2025-07-25 07:00:00", "08:00", 20.4)]
        [TestCase("2025-07-25 07:59:59", "08:00", 20.4)]
        [TestCase("2025-07-25 08:00:00", "17:00", 20.3)]
        [TestCase("2025-07-25 16:59:59", "17:00", 20.3)]
        [TestCase("2025-07-25 17:00:00", "22:00", 20.2)]
        [TestCase("2025-07-25 21:59:59", "22:00", 20.2)]
        [TestCase("2025-07-26 00:00:00", "07:30", 20.9)] // Saturday
        [TestCase("2025-07-26 07:29:59", "07:30", 20.9)]
        [TestCase("2025-07-26 07:30:00", "08:30", 20.8)]
        [TestCase("2025-07-26 08:29:59", "08:30", 20.8)]
        [TestCase("2025-07-26 08:30:00", "17:30", 20.7)]
        [TestCase("2025-07-26 17:29:59", "17:30", 20.7)]
        [TestCase("2025-07-26 17:30:00", "22:30", 20.6)]
        [TestCase("2025-07-26 22:29:59", "22:30", 20.6)]
        [TestCase("2025-07-27 00:00:00", "07:30", 20.9)] // Sunday
        [TestCase("2025-07-27 07:29:59", "07:30", 20.9)]
        [TestCase("2025-07-27 07:30:00", "08:30", 20.8)]
        [TestCase("2025-07-27 08:29:59", "08:30", 20.8)]
        [TestCase("2025-07-27 08:30:00", "17:30", 20.7)]
        [TestCase("2025-07-27 17:29:59", "17:30", 20.7)]
        [TestCase("2025-07-27 17:30:00", "22:30", 20.6)]
        [TestCase("2025-07-27 22:29:59", "22:30", 20.6)]

        public void GetNextSwitchingInterval_ReturnsCorrectResult(string isoDateTime, string expectedSwitchingTime, double expectedTemp)
        {
            // Arrange
            var service = new NeoHubService();

            // Act
            var result = service.GetNextSwitchingInterval(schedule, DateTime.Parse(isoDateTime));

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Time == TimeOnly.Parse(expectedSwitchingTime));
            Assert.That(result.TargetTemp == expectedTemp);
        }

        [TestCase("2025-07-21 22:00:00")] //Monday
        [TestCase("2025-07-25 22:00:00")] //Friday
        [TestCase("2025-07-26 22:30:00")] //Saturday
        [TestCase("2025-07-27 22:30:00")] //Sunday
        public void GetNextSwitchingInterval_NoRemainingIntervals_ReturnsNull(string isoDateTime)
        {
            // Arrange
            var service = new NeoHubService();

            // Act
            var result = service.GetNextSwitchingInterval(schedule, DateTime.Parse(isoDateTime));

            // Assert
            Assert.That(result, Is.Null);
        }
    }
}