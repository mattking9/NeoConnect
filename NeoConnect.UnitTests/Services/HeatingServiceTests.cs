using Microsoft.Extensions.Logging;
using Moq;

namespace NeoConnect.UnitTests
{
    [TestFixture]
    public class HeatingServiceTests
    {
        private HeatingService _heatingService;
        private Mock<INeoHubService> _mockNeoHubService;
        private Mock<IEmailService> _mockEmailService;
        private Mock<IDataService> _mockDataService;
        private Mock<ILogger<HeatingService>> _mockLogger;
        private CancellationTokenSource _cts;

        [SetUp]
        public void Setup()
        {
            _mockNeoHubService = new Mock<INeoHubService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockDataService = new Mock<IDataService>();
            _mockLogger = new Mock<ILogger<HeatingService>>();
            _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _cts = new CancellationTokenSource();
            _heatingService = new HeatingService(_mockLogger.Object, _mockNeoHubService.Object, _mockEmailService.Object, _mockDataService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _cts.Dispose();
        }

        [Test]
        public async Task GetDevices_WithAdvancedData_ReturnsFullyPopulatedDeviceList()
        {
            // arrange
            var neoDevices = new List<NeoDevice>()
            {
                new NeoDevice()
                {
                    DeviceId = 1,
                    ActiveProfile = 10,
                    ActualTemp = "10",
                    SetTemp = "20",
                    ZoneName = "Zone 1"
                },
                new NeoDevice()
                {
                    DeviceId = 2,
                    ActiveProfile = 11,
                    ActualTemp = "11",
                    SetTemp = "21",
                    ZoneName = "Zone 2"
                },
            };

            var profiles = new Dictionary<int, Profile>()
            {
                { 10, new Profile() { ProfileId = 1, ProfileName = "Profile 10" } },
                { 11, new Profile() { ProfileId = 1, ProfileName = "Profile 11" } },
            };

            var rocData = new Dictionary<string, int>()
            {
                { "Zone 1", 50 },
                { "Zone 2", 60 },
            };

            var eng = new Dictionary<string, EngineersData>()
            {
                { "Zone 1", new EngineersData() { MaxPreheatDuration = 2 } },
                { "Zone 2", new EngineersData() { MaxPreheatDuration = 3 } },
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(neoDevices);
            _mockNeoHubService.Setup(n => n.GetAllProfiles(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(n => n.GetROCData(It.IsAny<INeoConnection>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>())).ReturnsAsync(rocData);
            _mockNeoHubService.Setup(n => n.GetEngineersData(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(eng);


            var service = new HeatingService(_mockLogger.Object, _mockNeoHubService.Object, _mockEmailService.Object, _mockDataService.Object);

            // act
            var result = await service.GetDevices(true, _cts.Token);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            var item1 = result.FirstOrDefault(r => r.DeviceId == 1);
            Assert.That(item1, Is.Not.Null);
            Assert.That(item1.ZoneName, Is.EqualTo("Zone 1"));
            Assert.That(item1.ProfileName, Is.EqualTo("Profile 10"));
            Assert.That(item1.ActualTemp, Is.EqualTo(10));
            Assert.That(item1.SetTemp, Is.EqualTo(20));
            Assert.That(item1.MaxPreheatHours, Is.EqualTo(2));
            Assert.That(item1.RoC, Is.EqualTo(50));
            var item2 = result.FirstOrDefault(r => r.DeviceId == 2);
            Assert.That(item2, Is.Not.Null);
            Assert.That(item2.ZoneName, Is.EqualTo("Zone 2"));
            Assert.That(item2.ProfileName, Is.EqualTo("Profile 11"));
            Assert.That(item2.ActualTemp, Is.EqualTo(11));
            Assert.That(item2.SetTemp, Is.EqualTo(21));
            Assert.That(item2.MaxPreheatHours, Is.EqualTo(3));
            Assert.That(item2.RoC, Is.EqualTo(60));
        }

        [Test]
        public async Task GetDevices_WithoutAdvancedData_ReturnsPartiallyPopulatedDeviceList()
        {
            // arrange
            var neoDevices = new List<NeoDevice>()
            {
                new NeoDevice()
                {
                    DeviceId = 1,
                    ActiveProfile = 10,
                    ActualTemp = "10",
                    SetTemp = "20",
                    ZoneName = "Zone 1"
                },
                new NeoDevice()
                {
                    DeviceId = 2,
                    ActiveProfile = 11,
                    ActualTemp = "11",
                    SetTemp = "21",
                    ZoneName = "Zone 2"
                },
            };            

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(neoDevices);

            var service = new HeatingService(_mockLogger.Object, _mockNeoHubService.Object, _mockEmailService.Object, _mockDataService.Object);

            // act
            var result = await service.GetDevices(false, _cts.Token);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            var item1 = result.FirstOrDefault(r => r.DeviceId == 1);
            Assert.That(item1, Is.Not.Null);
            Assert.That(item1.ZoneName, Is.EqualTo("Zone 1"));
            Assert.That(item1.ProfileName, Is.Null);
            Assert.That(item1.ActualTemp, Is.EqualTo(10));
            Assert.That(item1.SetTemp, Is.EqualTo(20));
            Assert.That(item1.MaxPreheatHours, Is.Null);
            Assert.That(item1.RoC, Is.Null);
            var item2 = result.FirstOrDefault(r => r.DeviceId == 2);
            Assert.That(item2, Is.Not.Null);
            Assert.That(item2.ZoneName, Is.EqualTo("Zone 2"));
            Assert.That(item2.ProfileName, Is.Null);
            Assert.That(item2.ActualTemp, Is.EqualTo(11));
            Assert.That(item2.SetTemp, Is.EqualTo(21));
            Assert.That(item2.MaxPreheatHours, Is.Null);
            Assert.That(item2.RoC, Is.Null);

            _mockNeoHubService.Verify(n => n.GetAllProfiles(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockNeoHubService.Verify(n => n.GetROCData(It.IsAny<INeoConnection>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockNeoHubService.Verify(n => n.GetEngineersData(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>()), Times.Never);


        }
        
        [Test]
        public async Task GetDeviceHistory_WithMultipleDevices_ReturnsCorrectlyFormattedHistoryGrid()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15);
            var deviceStates = new List<NeoConnect.DataAccess.DeviceStateEntity>()
            {
                new() { DeviceId = 1, DeviceName = "Living Room", HeatOn = true, PreheatActive = false, Timestamp = testDate.AddHours(8) },
                new() { DeviceId = 1, DeviceName = "Living Room", HeatOn = false, PreheatActive = false, Timestamp = testDate.AddHours(9) },
                new() { DeviceId = 2, DeviceName = "Bedroom", HeatOn = false, PreheatActive = true, Timestamp = testDate.AddHours(6).AddMinutes(30) },
            };

            _mockDataService.Setup(d => d.GetDeviceData(testDate)).ReturnsAsync(deviceStates);

            // act
            var result = await _heatingService.GetDeviceHistory(testDate);

            // assert
            Assert.That(result, Is.Not.Null);
            var resultList = result.ToList();
            Assert.That(resultList.Count, Is.EqualTo(2));

            var livingRoom = resultList.FirstOrDefault(d => d.DeviceName == "Living Room");
            Assert.That(livingRoom, Is.Not.Null);
            Assert.That(livingRoom.History[32], Is.EqualTo(1)); // 8:00 AM (8 * 4 = 32)
            Assert.That(livingRoom.History[36], Is.EqualTo(0)); // 9:00 AM (9 * 4 = 36)

            var bedroom = resultList.FirstOrDefault(d => d.DeviceName == "Bedroom");
            Assert.That(bedroom, Is.Not.Null);
            Assert.That(bedroom.History[26], Is.EqualTo(2)); // 6:30 AM (6 * 4 + 2 = 26)
        }

        [Test]
        public async Task GetDeviceHistory_WithNoData_ReturnsEmptyList()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15);
            _mockDataService.Setup(d => d.GetDeviceData(testDate)).ReturnsAsync(new List<NeoConnect.DataAccess.DeviceStateEntity>());

            // act
            var result = await _heatingService.GetDeviceHistory(testDate);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task GetDeviceHistory_WithGapsInData_FillsGapsWithNegativeOne()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15);
            var deviceStates = new List<NeoConnect.DataAccess.DeviceStateEntity>()
            {
                new() { DeviceId = 1, DeviceName = "Living Room", HeatOn = true, PreheatActive = false, Timestamp = testDate.AddHours(8) },
                new() { DeviceId = 1, DeviceName = "Living Room", HeatOn = false, PreheatActive = false, Timestamp = testDate.AddHours(10) },
            };

            _mockDataService.Setup(d => d.GetDeviceData(testDate)).ReturnsAsync(deviceStates);

            // act
            var result = await _heatingService.GetDeviceHistory(testDate);

            // assert
            Assert.That(result, Is.Not.Null);
            var livingRoom = result.First();
            
            // Check that gaps before first entry are filled with -1
            for (int i = 0; i < 32; i++)
            {
                Assert.That(livingRoom.History[i], Is.EqualTo(-1));
            }

            // Check the actual data points
            Assert.That(livingRoom.History[32], Is.EqualTo(1)); // 8:00 AM
            
            // Check that gaps between entries are filled with -1
            for (int i = 33; i < 40; i++)
            {
                Assert.That(livingRoom.History[i], Is.EqualTo(-1));
            }

            Assert.That(livingRoom.History[40], Is.EqualTo(0)); // 10:00 AM
        }

        [Test]
        public async Task GetSchedules_WithMultipleProfiles_ReturnsAllSchedules()
        {
            // arrange
            var profiles = new Dictionary<int, Profile>()
            {
                { 1, new Profile() 
                    { 
                        ProfileId = 1, 
                        ProfileName = "Living Room",
                        Schedule = new ProfileSchedule()
                        {
                            Weekdays = new ProfileScheduleGroup()
                            {
                                Wake = new object[] { "06:00", "20.0" },
                                Leave = new object[] { "08:00", "16.0" },
                                Return = new object[] { "17:00", "21.0" },
                                Sleep = new object[] { "22:00", "18.0" }
                            },
                            Weekends = new ProfileScheduleGroup()
                            {
                                Wake = new object[] { "08:00", "20.0" },
                                Leave = new object[] { "10:00", "16.0" },
                                Return = new object[] { "16:00", "21.0" },
                                Sleep = new object[] { "23:00", "18.0" }
                            }
                        }
                    } 
                },
                { 2, new Profile() 
                    { 
                        ProfileId = 2, 
                        ProfileName = "Bedroom",
                        Schedule = new ProfileSchedule()
                        {
                            Weekdays = new ProfileScheduleGroup()
                            {
                                Wake = new object[] { "07:00", "19.0" },
                                Leave = new object[] { "09:00", "15.0" },
                                Return = new object[] { "18:00", "20.0" },
                                Sleep = new object[] { "23:00", "17.0" }
                            },
                            Weekends = new ProfileScheduleGroup()
                            {
                                Wake = new object[] { "09:00", "19.0" },
                                Leave = new object[] { "11:00", "15.0" },
                                Return = new object[] { "17:00", "20.0" },
                                Sleep = new object[] { "00:00", "17.0" }
                            }
                        }
                    } 
                }
            };

            _mockNeoHubService.Setup(n => n.GetAllProfiles(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(profiles);

            // act
            var result = await _heatingService.GetSchedules(_cts.Token);

            // assert
            Assert.That(result, Is.Not.Null);
            var schedules = result.ToList();
            Assert.That(schedules.Count, Is.EqualTo(2));

            var livingRoom = schedules.FirstOrDefault(s => s.ScheduleName == "Living Room");
            Assert.That(livingRoom, Is.Not.Null);
            Assert.That(livingRoom.Intervals.Length, Is.EqualTo(8));
            Assert.That(livingRoom.Intervals[0].TargetTemp, Is.EqualTo(20.0)); // Weekday Wake
            Assert.That(livingRoom.Intervals[4].TargetTemp, Is.EqualTo(20.0)); // Weekend Wake

            var bedroom = schedules.FirstOrDefault(s => s.ScheduleName == "Bedroom");
            Assert.That(bedroom, Is.Not.Null);
            Assert.That(bedroom.Intervals.Length, Is.EqualTo(8));
        }

        [Test]
        public async Task GetSchedules_WithNoProfiles_ReturnsEmptyList()
        {
            // arrange
            _mockNeoHubService.Setup(n => n.GetAllProfiles(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<int, Profile>());

            // act
            var result = await _heatingService.GetSchedules(_cts.Token);

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task BoostTowelRailWhenBathroomIsCold_WhenBathroomNotFound_LogsAndReturns()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Living Room" }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            // act
            await _heatingService.BoostTowelRailWhenBathroomIsCold(_cts.Token);

            // assert
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Device named 'Bathroom' was not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

            _mockNeoHubService.Verify(n => n.Boost(It.IsAny<INeoConnection>(), It.IsAny<string[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task BoostTowelRailWhenBathroomIsCold_WhenTowelRailNotFound_LogsAndReturns()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Bathroom", ActualTemp = "18", SetTemp = "20" }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            // act
            await _heatingService.BoostTowelRailWhenBathroomIsCold(_cts.Token);

            // assert
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Device named 'Towel Rail' was not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

            _mockNeoHubService.Verify(n => n.Boost(It.IsAny<INeoConnection>(), It.IsAny<string[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task BoostTowelRailWhenBathroomIsCold_WhenBathroomIsOffline_LogsAndReturns()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Bathroom", ActualTemp = "18", SetTemp = "20", IsOffline = true },
                new() { DeviceId = 2, ZoneName = "Towel Rail" }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            // act
            await _heatingService.BoostTowelRailWhenBathroomIsCold(_cts.Token);

            // assert
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Bathroom is in an inactive state")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

            _mockNeoHubService.Verify(n => n.Boost(It.IsAny<INeoConnection>(), It.IsAny<string[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task BoostTowelRailWhenBathroomIsCold_WhenBathroomIsStandby_LogsAndReturns()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Bathroom", ActualTemp = "18", SetTemp = "20", IsStandby = true },
                new() { DeviceId = 2, ZoneName = "Towel Rail" }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            // act
            await _heatingService.BoostTowelRailWhenBathroomIsCold(_cts.Token);

            // assert
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Bathroom is in an inactive state")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

            _mockNeoHubService.Verify(n => n.Boost(It.IsAny<INeoConnection>(), It.IsAny<string[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task BoostTowelRailWhenBathroomIsCold_WhenSetTempTooLow_LogsAndReturns()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Bathroom", ActualTemp = "10", SetTemp = "12", ActiveProfile = 1 },
                new() { DeviceId = 2, ZoneName = "Towel Rail" }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            // act
            await _heatingService.BoostTowelRailWhenBathroomIsCold(_cts.Token);

            // assert
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Bathroom is in an inactive state")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

            _mockNeoHubService.Verify(n => n.Boost(It.IsAny<INeoConnection>(), It.IsAny<string[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task BoostTowelRailWhenBathroomIsCold_WhenNoComfortLevelsRemaining_LogsAndReturns()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Bathroom", ActualTemp = "18", SetTemp = "20", ActiveProfile = 1 },
                new() { DeviceId = 2, ZoneName = "Towel Rail" }
            };

            var profiles = new Dictionary<int, Profile>()
            {
                { 1, new Profile() { ProfileId = 1, ProfileName = "Bathroom Profile", Schedule = new ProfileSchedule() } }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(n => n.GetAllProfiles(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(n => n.GetNextComfortLevel(It.IsAny<ProfileSchedule>(), It.IsAny<DateTime>())).Returns((ComfortLevel)null);

            // act
            await _heatingService.BoostTowelRailWhenBathroomIsCold(_cts.Token);

            // assert
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Bathroom has no more comfort levels today")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

            _mockNeoHubService.Verify(n => n.Boost(It.IsAny<INeoConnection>(), It.IsAny<string[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task BoostTowelRailWhenBathroomIsCold_WhenTemperatureDifferenceIsGreaterThanOne_BoostsAndSendsEmail()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Bathroom", ActualTemp = "18", SetTemp = "20", ActiveProfile = 1 },
                new() { DeviceId = 2, ZoneName = "Towel Rail" }
            };

            var profiles = new Dictionary<int, Profile>()
            {
                { 1, new Profile() { ProfileId = 1, ProfileName = "Bathroom Profile", Schedule = new ProfileSchedule() } }
            };

            var comfortLevel = new ComfortLevel(new object[] { "08:00", "20.0" });

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(n => n.GetAllProfiles(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(n => n.GetNextComfortLevel(It.IsAny<ProfileSchedule>(), It.IsAny<DateTime>())).Returns(comfortLevel);

            // act
            await _heatingService.BoostTowelRailWhenBathroomIsCold(_cts.Token);

            // assert
            _mockNeoHubService.Verify(n => n.Boost(
                It.IsAny<INeoConnection>(), 
                It.Is<string[]>(zones => zones.Contains("Towel Rail")), 
                1, 
                It.IsAny<CancellationToken>()), Times.Once);

            _mockEmailService.Verify(e => e.SendInfoEmail(
                It.Is<string>(msg => msg.Contains("Boosted") && msg.Contains("Towel Rail")), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task BoostTowelRailWhenBathroomIsCold_WhenTemperatureDifferenceLessThanOne_LogsAndDoesNotBoost()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Bathroom", ActualTemp = "19.5", SetTemp = "20", ActiveProfile = 1 },
                new() { DeviceId = 2, ZoneName = "Towel Rail" }
            };

            var profiles = new Dictionary<int, Profile>()
            {
                { 1, new Profile() { ProfileId = 1, ProfileName = "Bathroom Profile", Schedule = new ProfileSchedule() } }
            };

            var comfortLevel = new ComfortLevel(new object[] { "08:00", "20.0" });

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);
            _mockNeoHubService.Setup(n => n.GetAllProfiles(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
            _mockNeoHubService.Setup(n => n.GetNextComfortLevel(It.IsAny<ProfileSchedule>(), It.IsAny<DateTime>())).Returns(comfortLevel);

            // act
            await _heatingService.BoostTowelRailWhenBathroomIsCold(_cts.Token);

            // assert
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Bathroom Boost not required this time")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

            _mockNeoHubService.Verify(n => n.Boost(It.IsAny<INeoConnection>(), It.IsAny<string[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockEmailService.Verify(e => e.SendInfoEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }


        [Test]
        public async Task GlobalHold_WithActiveThermostats_AppliesHoldToAllDevices()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Living Room", IsThermostat = true, IsOffline = false, ActiveProfile = 1, IsStandby = false, SetTemp = "20" },
                new() { DeviceId = 2, ZoneName = "Bedroom", IsThermostat = true, IsOffline = false, ActiveProfile = 2, IsStandby = false, SetTemp = "19" },
                new() { DeviceId = 3, ZoneName = "Kitchen", IsThermostat = true, IsOffline = false, ActiveProfile = 3, IsStandby = false, SetTemp = "21" }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            var adjustment = -1.0;
            var holdHours = 2;

            // act
            await _heatingService.GlobalHold(adjustment, holdHours, _cts.Token);

            // assert
            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                "ReduceWhenWarm",
                It.Is<string[]>(zones => zones.Contains("Living Room")),
                19.0,
                holdHours,
                It.IsAny<CancellationToken>()), Times.Once);

            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                "ReduceWhenWarm",
                It.Is<string[]>(zones => zones.Contains("Bedroom")),
                18.0,
                holdHours,
                It.IsAny<CancellationToken>()), Times.Once);

            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                "ReduceWhenWarm",
                It.Is<string[]>(zones => zones.Contains("Kitchen")),
                20.0,
                holdHours,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GlobalHold_WithPositiveAdjustment_IncreasesTemperatures()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Living Room", IsThermostat = true, IsOffline = false, ActiveProfile = 1, IsStandby = false, SetTemp = "20" }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            var adjustment = 2.5;
            var holdHours = 3;

            // act
            await _heatingService.GlobalHold(adjustment, holdHours, _cts.Token);

            // assert
            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                "ReduceWhenWarm",
                It.Is<string[]>(zones => zones.Contains("Living Room")),
                22.5,
                holdHours,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GlobalHold_WithNegativeAdjustment_DecreasesTemperatures()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Living Room", IsThermostat = true, IsOffline = false, ActiveProfile = 1, IsStandby = false, SetTemp = "20" }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            var adjustment = -3.0;
            var holdHours = 1;

            // act
            await _heatingService.GlobalHold(adjustment, holdHours, _cts.Token);

            // assert
            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                "ReduceWhenWarm",
                It.Is<string[]>(zones => zones.Contains("Living Room")),
                17.0,
                holdHours,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GlobalHold_FiltersOutOfflineDevices()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Living Room", IsThermostat = true, IsOffline = false, ActiveProfile = 1, IsStandby = false, SetTemp = "20" },
                new() { DeviceId = 2, ZoneName = "Bedroom", IsThermostat = true, IsOffline = true, ActiveProfile = 1, IsStandby = false, SetTemp = "19" }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            var adjustment = -1.0;
            var holdHours = 2;

            // act
            await _heatingService.GlobalHold(adjustment, holdHours, _cts.Token);

            // assert
            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                It.IsAny<string>(),
                It.Is<string[]>(zones => zones.Contains("Living Room")),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                It.IsAny<string>(),
                It.Is<string[]>(zones => zones.Contains("Bedroom")),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task GlobalHold_FiltersOutStandbyDevices()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Living Room", IsThermostat = true, IsOffline = false, ActiveProfile = 1, IsStandby = false, SetTemp = "20" },
                new() { DeviceId = 2, ZoneName = "Bedroom", IsThermostat = true, IsOffline = false, ActiveProfile = 1, IsStandby = true, SetTemp = "19" }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            var adjustment = -1.0;
            var holdHours = 2;

            // act
            await _heatingService.GlobalHold(adjustment, holdHours, _cts.Token);

            // assert
            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                It.IsAny<string>(),
                It.Is<string[]>(zones => zones.Contains("Living Room")),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                It.IsAny<string>(),
                It.Is<string[]>(zones => zones.Contains("Bedroom")),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task GlobalHold_FiltersOutDevicesWithZeroActiveProfile()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Living Room", IsThermostat = true, IsOffline = false, ActiveProfile = 1, IsStandby = false, SetTemp = "20" },
                new() { DeviceId = 2, ZoneName = "Bedroom", IsThermostat = true, IsOffline = false, ActiveProfile = 0, IsStandby = false, SetTemp = "19" }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            var adjustment = -1.0;
            var holdHours = 2;

            // act
            await _heatingService.GlobalHold(adjustment, holdHours, _cts.Token);

            // assert
            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                It.IsAny<string>(),
                It.Is<string[]>(zones => zones.Contains("Living Room")),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                It.IsAny<string>(),
                It.Is<string[]>(zones => zones.Contains("Bedroom")),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task GlobalHold_FiltersOutNonThermostatDevices()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Living Room", IsThermostat = true, IsOffline = false, ActiveProfile = 1, IsStandby = false, SetTemp = "20" },
                new() { DeviceId = 2, ZoneName = "Towel Rail", IsThermostat = false, IsOffline = false, ActiveProfile = 1, IsStandby = false, SetTemp = "19" }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            var adjustment = -1.0;
            var holdHours = 2;

            // act
            await _heatingService.GlobalHold(adjustment, holdHours, _cts.Token);

            // assert
            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                It.IsAny<string>(),
                It.Is<string[]>(zones => zones.Contains("Living Room")),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                It.IsAny<string>(),
                It.Is<string[]>(zones => zones.Contains("Towel Rail")),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task GlobalHold_WithNoValidDevices_DoesNotCallHold()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Living Room", IsThermostat = true, IsOffline = true, ActiveProfile = 1, IsStandby = false, SetTemp = "20" },
                new() { DeviceId = 2, ZoneName = "Bedroom", IsThermostat = false, IsOffline = false, ActiveProfile = 1, IsStandby = false, SetTemp = "19" },
                new() { DeviceId = 3, ZoneName = "Kitchen", IsThermostat = true, IsOffline = false, ActiveProfile = 0, IsStandby = false, SetTemp = "18" }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            var adjustment = -1.0;
            var holdHours = 2;

            // act
            await _heatingService.GlobalHold(adjustment, holdHours, _cts.Token);

            // assert
            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                It.IsAny<string>(),
                It.IsAny<string[]>(),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task GlobalHold_UsesCorrectHoldGroup()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Living Room", IsThermostat = true, IsOffline = false, ActiveProfile = 1, IsStandby = false, SetTemp = "20" }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            var adjustment = -1.0;
            var holdHours = 2;

            // act
            await _heatingService.GlobalHold(adjustment, holdHours, _cts.Token);

            // assert
            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                "ReduceWhenWarm",
                It.IsAny<string[]>(),
                It.IsAny<double>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GlobalHold_WithZeroAdjustment_AppliesOriginalTemperatures()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Living Room", IsThermostat = true, IsOffline = false, ActiveProfile = 1, IsStandby = false, SetTemp = "20" }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            var adjustment = 0.0;
            var holdHours = 2;

            // act
            await _heatingService.GlobalHold(adjustment, holdHours, _cts.Token);

            // assert
            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                "ReduceWhenWarm",
                It.Is<string[]>(zones => zones.Contains("Living Room")),
                20.0,
                holdHours,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GlobalHold_PassesCancellationTokenToNeoHub()
        {
            // arrange
            var devices = new List<NeoDevice>()
            {
                new() { DeviceId = 1, ZoneName = "Living Room", IsThermostat = true, IsOffline = false, ActiveProfile = 1, IsStandby = false, SetTemp = "20" }
            };

            _mockNeoHubService.Setup(n => n.GetDevices(It.IsAny<INeoConnection>(), It.IsAny<CancellationToken>())).ReturnsAsync(devices);

            var adjustment = -1.0;
            var holdHours = 2;

            // act
            await _heatingService.GlobalHold(adjustment, holdHours, _cts.Token);

            // assert
            _mockNeoHubService.Verify(n => n.GetDevices(
                It.IsAny<INeoConnection>(),
                _cts.Token), Times.Once);

            _mockNeoHubService.Verify(n => n.Hold(
                It.IsAny<INeoConnection>(),
                It.IsAny<string>(),
                It.IsAny<string[]>(),
                It.IsAny<double>(),
                It.IsAny<int>(),
                _cts.Token), Times.Once);
        }
    }
}