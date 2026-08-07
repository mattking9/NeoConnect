using Moq;
using NeoConnect.DataAccess;

namespace NeoConnect.UnitTests.Services
{
    [TestFixture]
    public class DataServiceTests
    {
        private Mock<IDeviceRepository> _mockDeviceRepository;
        private DataService _dataService;

        [SetUp]
        public void Setup()
        {
            _mockDeviceRepository = new Mock<IDeviceRepository>();
            _dataService = new DataService(_mockDeviceRepository.Object);
        }

        #region RefreshDeviceList Tests

        [Test]
        public void RefreshDeviceList_WithMultipleDevices_CallsRepositoryWithCorrectData()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device { DeviceId = 1, ZoneName = "Living Room" },
                new Device { DeviceId = 2, ZoneName = "Bedroom" },
                new Device { DeviceId = 3, ZoneName = "Kitchen" }
            };

            DeviceEntity[] capturedEntities = null;
            _mockDeviceRepository.Setup(r => r.AddDevices(It.IsAny<IEnumerable<DeviceEntity>>()))
                .Callback<IEnumerable<DeviceEntity>>(entities => capturedEntities = entities.ToArray());

            // act
            _dataService.RefreshDeviceList(devices);

            // assert
            _mockDeviceRepository.Verify(r => r.AddDevices(It.IsAny<IEnumerable<DeviceEntity>>()), Times.Once);
            Assert.That(capturedEntities, Is.Not.Null);
            Assert.That(capturedEntities.Length, Is.EqualTo(3));
            Assert.That(capturedEntities[0].DeviceId, Is.EqualTo(1));
            Assert.That(capturedEntities[0].DeviceName, Is.EqualTo("Living Room"));
            Assert.That(capturedEntities[1].DeviceId, Is.EqualTo(2));
            Assert.That(capturedEntities[1].DeviceName, Is.EqualTo("Bedroom"));
            Assert.That(capturedEntities[2].DeviceId, Is.EqualTo(3));
            Assert.That(capturedEntities[2].DeviceName, Is.EqualTo("Kitchen"));
        }

        [Test]
        public void RefreshDeviceList_WithSingleDevice_CallsRepositoryWithCorrectData()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device { DeviceId = 10, ZoneName = "Bathroom" }
            };

            DeviceEntity[] capturedEntities = null;
            _mockDeviceRepository.Setup(r => r.AddDevices(It.IsAny<IEnumerable<DeviceEntity>>()))
                .Callback<IEnumerable<DeviceEntity>>(entities => capturedEntities = entities.ToArray());

            // act
            _dataService.RefreshDeviceList(devices);

            // assert
            _mockDeviceRepository.Verify(r => r.AddDevices(It.IsAny<IEnumerable<DeviceEntity>>()), Times.Once);
            Assert.That(capturedEntities, Is.Not.Null);
            Assert.That(capturedEntities.Length, Is.EqualTo(1));
            Assert.That(capturedEntities[0].DeviceId, Is.EqualTo(10));
            Assert.That(capturedEntities[0].DeviceName, Is.EqualTo("Bathroom"));
        }

        [Test]
        public void RefreshDeviceList_WithEmptyList_CallsRepositoryWithEmptyCollection()
        {
            // arrange
            var devices = new List<Device>();

            DeviceEntity[] capturedEntities = null;
            _mockDeviceRepository.Setup(r => r.AddDevices(It.IsAny<IEnumerable<DeviceEntity>>()))
                .Callback<IEnumerable<DeviceEntity>>(entities => capturedEntities = entities.ToArray());

            // act
            _dataService.RefreshDeviceList(devices);

            // assert
            _mockDeviceRepository.Verify(r => r.AddDevices(It.IsAny<IEnumerable<DeviceEntity>>()), Times.Once);
            Assert.That(capturedEntities, Is.Not.Null);
            Assert.That(capturedEntities.Length, Is.EqualTo(0));
        }

        [Test]
        public void RefreshDeviceList_WithSpecialCharactersInZoneName_MapsCorrectly()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device { DeviceId = 1, ZoneName = "Master's Bedroom" },
                new Device { DeviceId = 2, ZoneName = "Kid's Room #1" }
            };

            DeviceEntity[] capturedEntities = null;
            _mockDeviceRepository.Setup(r => r.AddDevices(It.IsAny<IEnumerable<DeviceEntity>>()))
                .Callback<IEnumerable<DeviceEntity>>(entities => capturedEntities = entities.ToArray());

            // act
            _dataService.RefreshDeviceList(devices);

            // assert
            Assert.That(capturedEntities[0].DeviceName, Is.EqualTo("Master's Bedroom"));
            Assert.That(capturedEntities[1].DeviceName, Is.EqualTo("Kid's Room #1"));
        }

        #endregion

        #region AddDeviceData Tests

        [Test]
        public void AddDeviceData_WithMultipleThermostats_CallsRepositoryWithCorrectData()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device 
                { 
                    DeviceId = 1, 
                    ZoneName = "Living Room", 
                    IsThermostat = true,
                    SetTemp = 20.5,
                    ActualTemp = 19.5,
                    IsHeating = true,
                    IsPreheating = false
                },
                new Device 
                { 
                    DeviceId = 2, 
                    ZoneName = "Bedroom", 
                    IsThermostat = true,
                    SetTemp = 18.0,
                    ActualTemp = 17.5,
                    IsHeating = false,
                    IsPreheating = true
                }
            };

            DeviceStateEntity[] capturedStates = null;
            _mockDeviceRepository.Setup(r => r.AddDeviceData(It.IsAny<IEnumerable<DeviceStateEntity>>()))
                .Callback<IEnumerable<DeviceStateEntity>>(states => capturedStates = states.ToArray());

            // act
            _dataService.AddDeviceData(devices, 12.5);

            // assert
            _mockDeviceRepository.Verify(r => r.AddDeviceData(It.IsAny<IEnumerable<DeviceStateEntity>>()), Times.Once);
            Assert.That(capturedStates, Is.Not.Null);
            Assert.That(capturedStates.Length, Is.EqualTo(2));
            
            Assert.That(capturedStates[0].DeviceId, Is.EqualTo(1));
            Assert.That(capturedStates[0].SetTemp, Is.EqualTo(20.5));
            Assert.That(capturedStates[0].ActualTemp, Is.EqualTo(19.5));
            Assert.That(capturedStates[0].HeatOn, Is.True);
            Assert.That(capturedStates[0].PreheatActive, Is.False);
            Assert.That(capturedStates[0].OutsideTemp, Is.EqualTo(12.5));
            Assert.That(capturedStates[0].Timestamp, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));

            Assert.That(capturedStates[1].DeviceId, Is.EqualTo(2));
            Assert.That(capturedStates[1].SetTemp, Is.EqualTo(18.0));
            Assert.That(capturedStates[1].ActualTemp, Is.EqualTo(17.5));
            Assert.That(capturedStates[1].HeatOn, Is.False);
            Assert.That(capturedStates[1].PreheatActive, Is.True);
            Assert.That(capturedStates[1].OutsideTemp, Is.EqualTo(12.5));
        }

        [Test]
        public void AddDeviceData_WithDecimalTemperatures_ParsesCorrectly()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device 
                { 
                    DeviceId = 1, 
                    ZoneName = "Living Room", 
                    IsThermostat = true,
                    SetTemp = 20.75,
                    ActualTemp = 19.25
                }
            };

            DeviceStateEntity[] capturedStates = null;
            _mockDeviceRepository.Setup(r => r.AddDeviceData(It.IsAny<IEnumerable<DeviceStateEntity>>()))
                .Callback<IEnumerable<DeviceStateEntity>>(states => capturedStates = states.ToArray());

            // act
            _dataService.AddDeviceData(devices, 10.5);

            // assert
            Assert.That(capturedStates[0].SetTemp, Is.EqualTo(20.75));
            Assert.That(capturedStates[0].ActualTemp, Is.EqualTo(19.25));
        }

        [Test]
        public void AddDeviceData_WithNegativeOutsideTemperature_StoresCorrectly()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device 
                { 
                    DeviceId = 1, 
                    ZoneName = "Living Room", 
                    IsThermostat = true,
                    SetTemp = 20.0,
                    ActualTemp = 19.0
                }
            };

            DeviceStateEntity[] capturedStates = null;
            _mockDeviceRepository.Setup(r => r.AddDeviceData(It.IsAny<IEnumerable<DeviceStateEntity>>()))
                .Callback<IEnumerable<DeviceStateEntity>>(states => capturedStates = states.ToArray());

            // act
            _dataService.AddDeviceData(devices, -5.5);

            // assert
            Assert.That(capturedStates[0].OutsideTemp, Is.EqualTo(-5.5));
        }

        [Test]
        public void AddDeviceData_WithZeroOutsideTemperature_StoresCorrectly()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device 
                { 
                    DeviceId = 1, 
                    ZoneName = "Living Room", 
                    IsThermostat = true,
                    SetTemp = 20.0,
                    ActualTemp = 19.0
                }
            };

            DeviceStateEntity[] capturedStates = null;
            _mockDeviceRepository.Setup(r => r.AddDeviceData(It.IsAny<IEnumerable<DeviceStateEntity>>()))
                .Callback<IEnumerable<DeviceStateEntity>>(states => capturedStates = states.ToArray());

            // act
            _dataService.AddDeviceData(devices, 0.0);

            // assert
            Assert.That(capturedStates[0].OutsideTemp, Is.EqualTo(0.0));
        }

        [Test]
        public void AddDeviceData_WithEmptyDeviceList_CallsRepositoryWithEmptyCollection()
        {
            // arrange
            var devices = new List<Device>();

            DeviceStateEntity[] capturedStates = null;
            _mockDeviceRepository.Setup(r => r.AddDeviceData(It.IsAny<IEnumerable<DeviceStateEntity>>()))
                .Callback<IEnumerable<DeviceStateEntity>>(states => capturedStates = states.ToArray());

            // act
            _dataService.AddDeviceData(devices, 10.0);

            // assert
            _mockDeviceRepository.Verify(r => r.AddDeviceData(It.IsAny<IEnumerable<DeviceStateEntity>>()), Times.Once);
            Assert.That(capturedStates, Is.Not.Null);
            Assert.That(capturedStates.Length, Is.EqualTo(0));
        }

        [Test]
        public void AddDeviceData_SetsTimestampToUtcNow()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device 
                { 
                    DeviceId = 1, 
                    ZoneName = "Living Room", 
                    IsThermostat = true,
                    SetTemp = 20.0,
                    ActualTemp = 19.0
                }
            };

            DeviceStateEntity[] capturedStates = null;
            _mockDeviceRepository.Setup(r => r.AddDeviceData(It.IsAny<IEnumerable<DeviceStateEntity>>()))
                .Callback<IEnumerable<DeviceStateEntity>>(states => capturedStates = states.ToArray());

            var beforeCall = DateTime.UtcNow;

            // act
            _dataService.AddDeviceData(devices, 10.0);

            var afterCall = DateTime.UtcNow;

            // assert
            Assert.That(capturedStates[0].Timestamp, Is.GreaterThanOrEqualTo(beforeCall));
            Assert.That(capturedStates[0].Timestamp, Is.LessThanOrEqualTo(afterCall));
            Assert.That(capturedStates[0].Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void AddDeviceData_WithMixedHeatingStates_StoresCorrectly()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device 
                { 
                    DeviceId = 1, 
                    ZoneName = "Zone 1", 
                    IsThermostat = true,
                    SetTemp = 20.0,
                    ActualTemp = 19.0,
                    IsHeating = true,
                    IsPreheating = false
                },
                new Device 
                { 
                    DeviceId = 2, 
                    ZoneName = "Zone 2", 
                    IsThermostat = true,
                    SetTemp = 18.0,
                    ActualTemp = 17.0,
                    IsHeating = false,
                    IsPreheating = true
                },
                new Device 
                { 
                    DeviceId = 3, 
                    ZoneName = "Zone 3", 
                    IsThermostat = true,
                    SetTemp = 19.0,
                    ActualTemp = 19.0,
                    IsHeating = false,
                    IsPreheating = false
                }
            };

            DeviceStateEntity[] capturedStates = null;
            _mockDeviceRepository.Setup(r => r.AddDeviceData(It.IsAny<IEnumerable<DeviceStateEntity>>()))
                .Callback<IEnumerable<DeviceStateEntity>>(states => capturedStates = states.ToArray());

            // act
            _dataService.AddDeviceData(devices, 10.0);

            // assert
            Assert.That(capturedStates[0].HeatOn, Is.True);
            Assert.That(capturedStates[0].PreheatActive, Is.False);
            Assert.That(capturedStates[1].HeatOn, Is.False);
            Assert.That(capturedStates[1].PreheatActive, Is.True);
            Assert.That(capturedStates[2].HeatOn, Is.False);
            Assert.That(capturedStates[2].PreheatActive, Is.False);
        }

        #endregion

        #region GetDeviceData Tests

        [Test]
        public async Task GetDeviceData_WithValidDate_ReturnsDataFromRepository()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15);
            var expectedData = new List<DeviceStateEntity>
            {
                new DeviceStateEntity 
                { 
                    DeviceId = 1, 
                    DeviceName = "Living Room",
                    SetTemp = 20.0,
                    ActualTemp = 19.5,
                    HeatOn = true,
                    PreheatActive = false,
                    OutsideTemp = 10.0,
                    Timestamp = testDate.AddHours(8)
                },
                new DeviceStateEntity 
                { 
                    DeviceId = 2, 
                    DeviceName = "Bedroom",
                    SetTemp = 18.0,
                    ActualTemp = 17.5,
                    HeatOn = false,
                    PreheatActive = false,
                    OutsideTemp = 10.0,
                    Timestamp = testDate.AddHours(9)
                }
            };

            _mockDeviceRepository.Setup(r => r.GetDeviceData(testDate))
                .ReturnsAsync(expectedData);

            // act
            var result = await _dataService.GetDeviceData(testDate);

            // assert
            _mockDeviceRepository.Verify(r => r.GetDeviceData(testDate), Times.Once);
            Assert.That(result, Is.Not.Null);
            var resultList = result.ToList();
            Assert.That(resultList.Count, Is.EqualTo(2));
            Assert.That(resultList[0].DeviceId, Is.EqualTo(1));
            Assert.That(resultList[0].DeviceName, Is.EqualTo("Living Room"));
            Assert.That(resultList[1].DeviceId, Is.EqualTo(2));
            Assert.That(resultList[1].DeviceName, Is.EqualTo("Bedroom"));
        }

        [Test]
        public async Task GetDeviceData_WithNoDataForDate_ReturnsEmptyCollection()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15);
            _mockDeviceRepository.Setup(r => r.GetDeviceData(testDate))
                .ReturnsAsync(new List<DeviceStateEntity>());

            // act
            var result = await _dataService.GetDeviceData(testDate);

            // assert
            _mockDeviceRepository.Verify(r => r.GetDeviceData(testDate), Times.Once);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task GetDeviceData_WithFutureDate_CallsRepositoryWithCorrectDate()
        {
            // arrange
            var futureDate = DateTime.Today.AddDays(30);
            _mockDeviceRepository.Setup(r => r.GetDeviceData(futureDate))
                .ReturnsAsync(new List<DeviceStateEntity>());

            // act
            await _dataService.GetDeviceData(futureDate);

            // assert
            _mockDeviceRepository.Verify(r => r.GetDeviceData(futureDate), Times.Once);
        }

        [Test]
        public async Task GetDeviceData_WithPastDate_CallsRepositoryWithCorrectDate()
        {
            // arrange
            var pastDate = new DateTime(2020, 1, 1);
            _mockDeviceRepository.Setup(r => r.GetDeviceData(pastDate))
                .ReturnsAsync(new List<DeviceStateEntity>());

            // act
            await _dataService.GetDeviceData(pastDate);

            // assert
            _mockDeviceRepository.Verify(r => r.GetDeviceData(pastDate), Times.Once);
        }

        [Test]
        public async Task GetDeviceData_WithDateAtMidnight_CallsRepositoryWithCorrectDate()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15, 0, 0, 0);
            _mockDeviceRepository.Setup(r => r.GetDeviceData(testDate))
                .ReturnsAsync(new List<DeviceStateEntity>());

            // act
            await _dataService.GetDeviceData(testDate);

            // assert
            _mockDeviceRepository.Verify(r => r.GetDeviceData(testDate), Times.Once);
        }

        [Test]
        public async Task GetDeviceData_WithDateAtEndOfDay_CallsRepositoryWithCorrectDate()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15, 23, 59, 59);
            _mockDeviceRepository.Setup(r => r.GetDeviceData(testDate))
                .ReturnsAsync(new List<DeviceStateEntity>());

            // act
            await _dataService.GetDeviceData(testDate);

            // assert
            _mockDeviceRepository.Verify(r => r.GetDeviceData(testDate), Times.Once);
        }

        [Test]
        public async Task GetDeviceData_ReturnsDataInSameOrderAsRepository()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15);
            var expectedData = new List<DeviceStateEntity>
            {
                new DeviceStateEntity { DeviceId = 3, DeviceName = "Kitchen" },
                new DeviceStateEntity { DeviceId = 1, DeviceName = "Living Room" },
                new DeviceStateEntity { DeviceId = 2, DeviceName = "Bedroom" }
            };

            _mockDeviceRepository.Setup(r => r.GetDeviceData(testDate))
                .ReturnsAsync(expectedData);

            // act
            var result = await _dataService.GetDeviceData(testDate);

            // assert
            var resultList = result.ToList();
            Assert.That(resultList[0].DeviceId, Is.EqualTo(3));
            Assert.That(resultList[1].DeviceId, Is.EqualTo(1));
            Assert.That(resultList[2].DeviceId, Is.EqualTo(2));
        }

        [Test]
        public async Task GetDeviceData_WithLargeDataset_ReturnsAllData()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15);
            var largeDataset = Enumerable.Range(1, 1000).Select(i => new DeviceStateEntity
            {
                DeviceId = i,
                DeviceName = $"Device {i}",
                Timestamp = testDate.AddMinutes(i)
            }).ToList();

            _mockDeviceRepository.Setup(r => r.GetDeviceData(testDate))
                .ReturnsAsync(largeDataset);

            // act
            var result = await _dataService.GetDeviceData(testDate);

            // assert
            Assert.That(result.Count(), Is.EqualTo(1000));
        }

        #endregion

        #region Integration-Style Tests

        [Test]
        public void AddDeviceData_FollowedByGetDeviceData_MaintainsDataIntegrity()
        {
            // This test verifies the workflow where data is added and then retrieved
            // arrange
            var testDate = DateTime.Today;
            var devices = new List<Device>
            {
                new Device 
                { 
                    DeviceId = 1, 
                    ZoneName = "Living Room", 
                    IsThermostat = true,
                    SetTemp = 20.5,
                    ActualTemp = 19.5,
                    IsHeating = true,
                    IsPreheating = false
                }
            };

            DeviceStateEntity[] capturedStates = null;
            _mockDeviceRepository.Setup(r => r.AddDeviceData(It.IsAny<IEnumerable<DeviceStateEntity>>()))
                .Callback<IEnumerable<DeviceStateEntity>>(states => capturedStates = states.ToArray());

            // act
            _dataService.AddDeviceData(devices, 12.5);

            // assert - verify the data that would be stored matches expected format
            Assert.That(capturedStates, Is.Not.Null);
            Assert.That(capturedStates[0].DeviceId, Is.EqualTo(1));
            Assert.That(capturedStates[0].SetTemp, Is.EqualTo(20.5));
            Assert.That(capturedStates[0].ActualTemp, Is.EqualTo(19.5));
            Assert.That(capturedStates[0].HeatOn, Is.True);
            Assert.That(capturedStates[0].PreheatActive, Is.False);
            Assert.That(capturedStates[0].OutsideTemp, Is.EqualTo(12.5));
        }

        #endregion
    }
}