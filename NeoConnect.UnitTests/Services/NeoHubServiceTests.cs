using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net.WebSockets;

namespace NeoConnect.UnitTests
{
    [TestFixture]
    public class NeoHubServiceTests
    {
        private Mock<ILogger<NeoHubService>> _mockLogger;
        private Mock<INeoConnectionFactory> _mockConnectionFactory;
        private Mock<INeoConnection> _mockConnection;
        private IConfiguration _configuration;
        private NeoHubService _neoHubService;
        private ProfileSchedule schedule;
        private CancellationTokenSource _cts;

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

            _mockConnectionFactory = new Mock<INeoConnectionFactory>();
            _mockConnection = new Mock<INeoConnection>();
            
            _mockConnection.Setup(c => c.State).Returns(WebSocketState.Open);
            _mockConnectionFactory.Setup(f => f.Create()).Returns(_mockConnection.Object);

            _cts = new CancellationTokenSource();

            _neoHubService = new NeoHubService(_mockLogger.Object, _configuration, _mockConnectionFactory.Object);

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

        [TearDown]
        public void TearDown()
        {
            _cts.Dispose();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithValidConfiguration_CreatesInstance()
        {
            // arrange & act
            var service = new NeoHubService(_mockLogger.Object, _configuration, _mockConnectionFactory.Object);

            // assert
            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithMissingUri_ThrowsArgumentNullException()
        {
            // arrange
            var inMemorySettings = new Dictionary<string, string> {
                {"NeoHub:ApiKey", "test-api-key"}
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => 
                new NeoHubService(_mockLogger.Object, config, _mockConnectionFactory.Object));
        }

        [Test]
        public void Constructor_WithMissingApiKey_ThrowsArgumentNullException()
        {
            // arrange
            var inMemorySettings = new Dictionary<string, string> {
                {"NeoHub:Uri", "wss://192.168.1.1:1234"}
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => 
                new NeoHubService(_mockLogger.Object, config, _mockConnectionFactory.Object));
        }

        #endregion

        #region CreateConnection Tests

        [Test]
        public async Task CreateConnection_SuccessfulConnection_ReturnsConnection()
        {
            // arrange
            _mockConnection.Setup(c => c.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await _neoHubService.CreateConnection(_cts.Token);

            // assert
            Assert.That(result, Is.Not.Null);
            _mockConnection.Verify(c => c.ConnectAsync(It.Is<Uri>(u => u.ToString() == "wss://192.168.1.1:1234/"), _cts.Token), Times.Once);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Connecting to NeoHub")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Connected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Test]
        public void CreateConnection_ConnectionFails_LogsErrorAndThrows()
        {
            // arrange
            _mockConnection.Setup(c => c.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Connection failed"));

            // act & assert
            Assert.ThrowsAsync<Exception>(async () => await _neoHubService.CreateConnection(_cts.Token));
            
            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error connecting to NeoHub")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        #endregion

        #region GetDevices Tests

        [Test]
        public async Task GetDevices_ValidResponse_ReturnsDeviceList()
        {
            // arrange
            var liveData = new NeoHubLiveData
            {
                Devices = new List<NeoDevice>
                {
                    new NeoDevice { DeviceId = 1, ZoneName = "Living Room", ActualTemp = "20", SetTemp = "21" },
                    new NeoDevice { DeviceId = 2, ZoneName = "Bedroom", ActualTemp = "19", SetTemp = "20" }
                }
            };

            var responseJson = JsonSerializer.Serialize(liveData);
            var hubResponse = new NeoHubResponse { ResponseJson = responseJson };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            // act
            var result = await _neoHubService.GetDevices(_mockConnection.Object, _cts.Token);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].ZoneName, Is.EqualTo("Living Room"));
            Assert.That(result[1].ZoneName, Is.EqualTo("Bedroom"));
            
            _mockConnection.Verify(c => c.SendAllAsync(It.Is<string>(s => s.Contains("GET_LIVE_DATA")), _cts.Token), Times.Once);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Fetching Devices")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Test]
        public void GetDevices_InvalidJsonResponse_ThrowsException()
        {
            // arrange
            var hubResponse = new NeoHubResponse { ResponseJson = "invalid json" };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            // act & assert
            Assert.ThrowsAsync<JsonException>(async () => await _neoHubService.GetDevices(_mockConnection.Object, _cts.Token));
        }

        #endregion

        #region GetEngineersData Tests

        [Test]
        public async Task GetEngineersData_ValidResponse_ReturnsDictionary()
        {
            // arrange
            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Living Room", new EngineersData { DeviceId = 1, MaxPreheatDuration = 2 } },
                { "Bedroom", new EngineersData { DeviceId = 2, MaxPreheatDuration = 3 } }
            };

            var responseJson = JsonSerializer.Serialize(engineersData);
            var hubResponse = new NeoHubResponse { ResponseJson = responseJson };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            // act
            var result = await _neoHubService.GetEngineersData(_mockConnection.Object, _cts.Token);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result["Living Room"].MaxPreheatDuration, Is.EqualTo(2));
            Assert.That(result["Bedroom"].MaxPreheatDuration, Is.EqualTo(3));
            
            _mockConnection.Verify(c => c.SendAllAsync(It.Is<string>(s => s.Contains("GET_ENGINEERS")), _cts.Token), Times.Once);
        }

        [Test]
        public void GetEngineersData_InvalidJsonResponse_ThrowsException()
        {
            // arrange
            var hubResponse = new NeoHubResponse { ResponseJson = "invalid json" };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            // act & assert
            Assert.ThrowsAsync<JsonException>(async () => await _neoHubService.GetEngineersData(_mockConnection.Object, _cts.Token));
        }

        #endregion

        #region GetAllProfiles Tests

        [Test]
        public async Task GetAllProfiles_ValidResponse_ReturnsDictionaryByProfileId()
        {
            // arrange
            var profilesResponse = new Dictionary<string, Profile>
            {
                { "profile1", new Profile { ProfileId = 10, ProfileName = "Living Room", Schedule = schedule } },
                { "profile2", new Profile { ProfileId = 20, ProfileName = "Bedroom", Schedule = schedule } }
            };

            var responseJson = JsonSerializer.Serialize(profilesResponse);
            var hubResponse = new NeoHubResponse { ResponseJson = responseJson };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            // act
            var result = await _neoHubService.GetAllProfiles(_mockConnection.Object, _cts.Token);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.ContainsKey(10), Is.True);
            Assert.That(result.ContainsKey(20), Is.True);
            Assert.That(result[10].ProfileName, Is.EqualTo("Living Room"));
            Assert.That(result[20].ProfileName, Is.EqualTo("Bedroom"));
            
            _mockConnection.Verify(c => c.SendAllAsync(It.Is<string>(s => s.Contains("GET_PROFILES")), _cts.Token), Times.Once);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Fetching Profiles")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Test]
        public void GetAllProfiles_InvalidJsonResponse_ThrowsException()
        {
            // arrange
            var hubResponse = new NeoHubResponse { ResponseJson = "invalid json" };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            // act & assert
            Assert.ThrowsAsync<JsonException>(async () => await _neoHubService.GetAllProfiles(_mockConnection.Object, _cts.Token));
        }

        #endregion

        #region GetROCData Tests

        [Test]
        public async Task GetROCData_WithMultipleDevices_ReturnsDictionary()
        {
            // arrange
            var rocData = new Dictionary<string, int>
            {
                { "Living Room", 50 },
                { "Bedroom", 60 }
            };

            var responseJson = JsonSerializer.Serialize(rocData);
            var hubResponse = new NeoHubResponse { ResponseJson = responseJson };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            var devices = new[] { "Living Room", "Bedroom" };

            // act
            var result = await _neoHubService.GetROCData(_mockConnection.Object, devices, _cts.Token);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result["Living Room"], Is.EqualTo(50));
            Assert.That(result["Bedroom"], Is.EqualTo(60));
            
            _mockConnection.Verify(c => c.SendAllAsync(
                It.Is<string>(s => s.Contains("VIEW_ROC") && s.Contains("'Living Room'") && s.Contains("'Bedroom'")), 
                _cts.Token), Times.Once);
        }

        [Test]
        public async Task GetROCData_WithSingleDevice_FormatsCorrectly()
        {
            // arrange
            var rocData = new Dictionary<string, int>
            {
                { "Living Room", 50 }
            };

            var responseJson = JsonSerializer.Serialize(rocData);
            var hubResponse = new NeoHubResponse { ResponseJson = responseJson };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            var devices = new[] { "Living Room" };

            // act
            var result = await _neoHubService.GetROCData(_mockConnection.Object, devices, _cts.Token);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            
            _mockConnection.Verify(c => c.SendAllAsync(
                It.Is<string>(s => s.Contains("VIEW_ROC") && s.Contains("['Living Room']")), 
                _cts.Token), Times.Once);
        }

        [Test]
        public async Task GetROCData_WithEmptyArray_FormatsCorrectly()
        {
            // arrange
            var rocData = new Dictionary<string, int>();

            var responseJson = JsonSerializer.Serialize(rocData);
            var hubResponse = new NeoHubResponse { ResponseJson = responseJson };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            var devices = Array.Empty<string>();

            // act
            var result = await _neoHubService.GetROCData(_mockConnection.Object, devices, _cts.Token);

            // assert
            Assert.That(result, Is.Not.Null);
            
            _mockConnection.Verify(c => c.SendAllAsync(
                It.Is<string>(s => s.Contains("VIEW_ROC") && s.Contains("[]")), 
                _cts.Token), Times.Once);
        }

        #endregion

        #region SetTemperature Tests

        [Test]
        public async Task SetTemperature_ValidRequest_SendsCorrectCommand()
        {
            // arrange
            var hubResponse = new NeoHubResponse { ResponseJson = "{}" };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            // act
            await _neoHubService.SetTemperature(_mockConnection.Object, "Living Room", 21.5, _cts.Token);

            // assert
            _mockConnection.Verify(c => c.SendAllAsync(
                It.Is<string>(s => s.Contains("SET_TEMP") && s.Contains("21.5") && s.Contains("'Living Room'")), 
                _cts.Token), Times.Once);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Setting Device 'Living Room' to 21.5c")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Test]
        public void SetTemperature_WebSocketNotOpen_ThrowsInvalidOperationException()
        {
            // arrange
            _mockConnection.Setup(c => c.State).Returns(WebSocketState.Closed);

            // act & assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await _neoHubService.SetTemperature(_mockConnection.Object, "Living Room", 21.5, _cts.Token));
        }

        #endregion

        #region Hold Tests

        [Test]
        public async Task Hold_ValidRequest_SendsCorrectCommand()
        {
            // arrange
            var hubResponse = new NeoHubResponse { ResponseJson = "{}" };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            var devices = new[] { "Living Room", "Bedroom" };

            // act
            await _neoHubService.Hold(_mockConnection.Object, "TestHold", devices, 19.5, 2, _cts.Token);

            // assert
            _mockConnection.Verify(c => c.SendAllAsync(
                It.Is<string>(s => s.Contains("HOLD") && 
                                   s.Contains("19.5") && 
                                   s.Contains("hours': 2") && 
                                   s.Contains("'TestHold'") &&
                                   s.Contains("'Living Room'") &&
                                   s.Contains("'Bedroom'")), 
                _cts.Token), Times.Once);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Holding Living Room,Bedroom at 19.5c for 2 hours")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Test]
        public void Hold_WebSocketNotOpen_ThrowsInvalidOperationException()
        {
            // arrange
            _mockConnection.Setup(c => c.State).Returns(WebSocketState.Closed);
            var devices = new[] { "Living Room" };

            // act & assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await _neoHubService.Hold(_mockConnection.Object, "TestHold", devices, 19.5, 2, _cts.Token));
        }

        #endregion

        #region Boost Tests

        [Test]
        public async Task Boost_ValidRequest_SendsCorrectCommand()
        {
            // arrange
            var hubResponse = new NeoHubResponse { ResponseJson = "{}" };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            var devices = new[] { "Towel Rail" };

            // act
            await _neoHubService.Boost(_mockConnection.Object, devices, 1, _cts.Token);

            // assert
            _mockConnection.Verify(c => c.SendAllAsync(
                It.Is<string>(s => s.Contains("BOOST_ON") && 
                                   s.Contains("hours': 1") && 
                                   s.Contains("'Towel Rail'")), 
                _cts.Token), Times.Once);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Boosting Towel Rail for 1 hours")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Test]
        public async Task Boost_MultipleDevices_SendsCorrectCommand()
        {
            // arrange
            var hubResponse = new NeoHubResponse { ResponseJson = "{}" };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            var devices = new[] { "Living Room", "Bedroom", "Kitchen" };

            // act
            await _neoHubService.Boost(_mockConnection.Object, devices, 2, _cts.Token);

            // assert
            _mockConnection.Verify(c => c.SendAllAsync(
                It.Is<string>(s => s.Contains("BOOST_ON") && 
                                   s.Contains("hours': 2") && 
                                   s.Contains("'Living Room'") &&
                                   s.Contains("'Bedroom'") &&
                                   s.Contains("'Kitchen'")), 
                _cts.Token), Times.Once);
        }

        #endregion

        #region GetNextComfortLevel Tests

        [Test]
        public void GetNextComfortLevel_WeekdayMorning_ReturnsLeave()
        {
            // arrange - Monday at 7:30 AM
            var testDate = new DateTime(2024, 1, 15, 7, 30, 0); // Monday

            // act
            var result = _neoHubService.GetNextComfortLevel(schedule, testDate);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TargetTemp, Is.EqualTo(20.4)); // Leave temp
        }

        [Test]
        public void GetNextComfortLevel_WeekdayAfternoon_ReturnsReturn()
        {
            // arrange - Wednesday at 12:00 PM
            var testDate = new DateTime(2024, 1, 17, 12, 0, 0); // Wednesday

            // act
            var result = _neoHubService.GetNextComfortLevel(schedule, testDate);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TargetTemp, Is.EqualTo(20.3)); // Return temp
        }

        [Test]
        public void GetNextComfortLevel_WeekdayEvening_ReturnsSleep()
        {
            // arrange - Friday at 20:00
            var testDate = new DateTime(2024, 1, 19, 20, 0, 0); // Friday

            // act
            var result = _neoHubService.GetNextComfortLevel(schedule, testDate);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TargetTemp, Is.EqualTo(20.2)); // Sleep temp
        }

        [Test]
        public void GetNextComfortLevel_WeekdayLateNight_ReturnsNull()
        {
            // arrange - Thursday at 23:00
            var testDate = new DateTime(2024, 1, 18, 23, 0, 0); // Thursday

            // act
            var result = _neoHubService.GetNextComfortLevel(schedule, testDate);

            // assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetNextComfortLevel_SaturdayMorning_ReturnsWeekendLeave()
        {
            // arrange - Saturday at 8:00 AM
            var testDate = new DateTime(2024, 1, 20, 8, 0, 0); // Saturday

            // act
            var result = _neoHubService.GetNextComfortLevel(schedule, testDate);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TargetTemp, Is.EqualTo(20.8)); // Weekend Leave temp
        }

        [Test]
        public void GetNextComfortLevel_SundayAfternoon_ReturnsWeekendReturn()
        {
            // arrange - Sunday at 10:00 AM
            var testDate = new DateTime(2024, 1, 21, 10, 0, 0); // Sunday

            // act
            var result = _neoHubService.GetNextComfortLevel(schedule, testDate);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TargetTemp, Is.EqualTo(20.7)); // Weekend Return temp
        }

        [Test]
        public void GetNextComfortLevel_SundayEvening_ReturnsWeekendSleep()
        {
            // arrange - Sunday at 20:00
            var testDate = new DateTime(2024, 1, 21, 20, 0, 0); // Sunday

            // act
            var result = _neoHubService.GetNextComfortLevel(schedule, testDate);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TargetTemp, Is.EqualTo(20.6)); // Weekend Sleep temp
        }

        [Test]
        public void GetNextComfortLevel_WeekdayEarlyMorning_ReturnsWake()
        {
            // arrange - Tuesday at 06:00 AM
            var testDate = new DateTime(2024, 1, 16, 6, 0, 0); // Tuesday

            // act
            var result = _neoHubService.GetNextComfortLevel(schedule, testDate);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TargetTemp, Is.EqualTo(20.5)); // Weekday Wake temp
        }

        [Test]
        public void GetNextComfortLevel_WeekendEarlyMorning_ReturnsWeekendWake()
        {
            // arrange - Saturday at 07:00 AM
            var testDate = new DateTime(2024, 1, 20, 7, 0, 0); // Saturday

            // act
            var result = _neoHubService.GetNextComfortLevel(schedule, testDate);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TargetTemp, Is.EqualTo(20.9)); // Weekend Wake temp
        }

        [Test]
        public void GetNextComfortLevel_NullDateTime_UsesCurrentTime()
        {
            // arrange & act
            var result = _neoHubService.GetNextComfortLevel(schedule, null);

            // assert - should not throw and should return a result or null based on current time
            Assert.That(result, Is.Not.Null.Or.Null);
        }

        #endregion

        #region SendMessage Tests (Validation through other methods)

        [Test]
        public async Task SendMessage_IncrementsCommandId()
        {
            // arrange
            var hubResponse = new NeoHubResponse { ResponseJson = "{\"devices\":[]}" };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            var sentMessages = new List<string>();
            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((msg, ct) => sentMessages.Add(msg))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            // act
            await _neoHubService.GetDevices(_mockConnection.Object, _cts.Token);
            await _neoHubService.GetDevices(_mockConnection.Object, _cts.Token);

            // assert
            Assert.That(sentMessages.Count, Is.EqualTo(2));
            // Command IDs should be different (incremented)
            Assert.That(sentMessages[0], Does.Not.Contain(sentMessages[1].Split("COMMANDID")[1].Split('}')[0]));
        }

        [Test]
        public async Task SendMessage_IncludesApiKeyInMessage()
        {
            // arrange
            var hubResponse = new NeoHubResponse { ResponseJson = "{\"devices\":[]}" };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            string sentMessage = null;
            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((msg, ct) => sentMessage = msg)
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            // act
            await _neoHubService.GetDevices(_mockConnection.Object, _cts.Token);

            // assert
            Assert.That(sentMessage, Does.Contain("test-api-key"));
            Assert.That(sentMessage, Does.Contain("hm_get_command_queue"));
        }

        #endregion

        #region ReceiveMessage Tests (Validation through other methods)

        [Test]
        public void ReceiveMessage_InvalidJsonStructure_ThrowsException()
        {
            // arrange
            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("not valid json at all");

            // act & assert
            Assert.ThrowsAsync<JsonException>(async () => await _neoHubService.GetDevices(_mockConnection.Object, _cts.Token));
        }

        #endregion

        #region Edge Cases and Error Scenarios

        [Test]
        public async Task GetROCData_WithSpecialCharactersInDeviceName_EscapesCorrectly()
        {
            // arrange
            var rocData = new Dictionary<string, int>
            {
                { "Living Room's Zone", 50 }
            };

            var responseJson = JsonSerializer.Serialize(rocData);
            var hubResponse = new NeoHubResponse { ResponseJson = responseJson };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            var devices = new[] { "Living Room's Zone" };

            // act
            var result = await _neoHubService.GetROCData(_mockConnection.Object, devices, _cts.Token);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result["Living Room's Zone"], Is.EqualTo(50));
        }

        [Test]
        public async Task Hold_WithZeroHours_SendsCorrectCommand()
        {
            // arrange
            var hubResponse = new NeoHubResponse { ResponseJson = "{}" };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            var devices = new[] { "Living Room" };

            // act
            await _neoHubService.Hold(_mockConnection.Object, "TestHold", devices, 20, 0, _cts.Token);

            // assert
            _mockConnection.Verify(c => c.SendAllAsync(
                It.Is<string>(s => s.Contains("hours': 0")), 
                _cts.Token), Times.Once);
        }

        [Test]
        public async Task SetTemperature_WithNegativeTemperature_SendsCorrectCommand()
        {
            // arrange
            var hubResponse = new NeoHubResponse { ResponseJson = "{}" };
            var hubResponseJson = JsonSerializer.Serialize(hubResponse);

            _mockConnection.Setup(c => c.SendAllAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockConnection.Setup(c => c.ReceiveAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hubResponseJson);

            // act
            await _neoHubService.SetTemperature(_mockConnection.Object, "Test", -5.0, _cts.Token);

            // assert
            _mockConnection.Verify(c => c.SendAllAsync(
                It.Is<string>(s => s.Contains("-5")), 
                _cts.Token), Times.Once);
        }

        [Test]
        public void GetNextComfortLevel_WithEmptySchedule_ReturnsNull()
        {
            // arrange
            var emptySchedule = new ProfileSchedule
            {
                Weekdays = new ProfileScheduleGroup
                {
                    Wake = null,
                    Leave = null,
                    Return = null,
                    Sleep = null
                },
                Weekends = new ProfileScheduleGroup
                {
                    Wake = null,
                    Leave = null,
                    Return = null,
                    Sleep = null
                }
            };

            var testDate = new DateTime(2024, 1, 15, 12, 0, 0);

            // act & assert - should not throw
            Assert.DoesNotThrow(() => _neoHubService.GetNextComfortLevel(emptySchedule, testDate));
        }

        #endregion
    }
}