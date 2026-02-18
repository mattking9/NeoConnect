using Microsoft.AspNetCore.Mvc;
using Moq;

namespace NeoConnect.UnitTests.Controllers
{
    [TestFixture]
    public class DevicesControllerTests
    {
        private Mock<IHeatingService> _mockHeatingService;
        private DevicesController _controller;
        private CancellationTokenSource _cts;

        [SetUp]
        public void Setup()
        {
            _mockHeatingService = new Mock<IHeatingService>();
            _controller = new DevicesController(_mockHeatingService.Object);
            _cts = new CancellationTokenSource();
        }

        [TearDown]
        public void TearDown()
        {
            _cts.Dispose();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithValidDependencies_CreatesInstance()
        {
            // arrange & act
            var controller = new DevicesController(_mockHeatingService.Object);

            // assert
            Assert.That(controller, Is.Not.Null);
        }

        #endregion

        #region GetAllDevices Tests

        [Test]
        public async Task GetAllDevices_WithIncludeAdvancedDataTrue_CallsServiceWithCorrectParameter()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device { DeviceId = 1, ZoneName = "Living Room", IsThermostat = true }
            };

            _mockHeatingService.Setup(h => h.GetDevices(true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(devices);

            // act
            var result = await _controller.GetAllDevices(true);

            // assert
            _mockHeatingService.Verify(h => h.GetDevices(true, CancellationToken.None), Times.Once);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetAllDevices_WithIncludeAdvancedDataFalse_CallsServiceWithCorrectParameter()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device { DeviceId = 1, ZoneName = "Living Room", IsThermostat = true }
            };

            _mockHeatingService.Setup(h => h.GetDevices(false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(devices);

            // act
            var result = await _controller.GetAllDevices(false);

            // assert
            _mockHeatingService.Verify(h => h.GetDevices(false, CancellationToken.None), Times.Once);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetAllDevices_ReturnsDevicesFromService()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device { DeviceId = 1, ZoneName = "Living Room", IsThermostat = true },
                new Device { DeviceId = 2, ZoneName = "Bedroom", IsThermostat = true },
                new Device { DeviceId = 3, ZoneName = "Kitchen", IsThermostat = false }
            };

            _mockHeatingService.Setup(h => h.GetDevices(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(devices);

            // act
            var result = await _controller.GetAllDevices(false);

            // assert
            var resultList = result.ToList();
            Assert.That(resultList.Count, Is.EqualTo(3));
            Assert.That(resultList.Any(d => d.ZoneName == "Living Room"), Is.True);
            Assert.That(resultList.Any(d => d.ZoneName == "Bedroom"), Is.True);
            Assert.That(resultList.Any(d => d.ZoneName == "Kitchen"), Is.True);
        }

        [Test]
        public async Task GetAllDevices_OrdersHeatingDevicesFirst()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device { DeviceId = 1, ZoneName = "Not Heating", IsHeating = false, IsPreheating = false, TimerOn = false, IsThermostat = true },
                new Device { DeviceId = 2, ZoneName = "Heating", IsHeating = true, IsPreheating = false, TimerOn = false, IsThermostat = true },
                new Device { DeviceId = 3, ZoneName = "Preheating", IsHeating = false, IsPreheating = true, TimerOn = false, IsThermostat = true }
            };

            _mockHeatingService.Setup(h => h.GetDevices(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(devices);

            // act
            var result = await _controller.GetAllDevices(false);

            // assert
            var resultList = result.ToList();
            // Devices that are heating, preheating, or have timer on should come first
            Assert.That(resultList[0].ZoneName, Is.EqualTo("Heating").Or.EqualTo("Preheating"));
            Assert.That(resultList[1].ZoneName, Is.EqualTo("Heating").Or.EqualTo("Preheating"));
            Assert.That(resultList[2].ZoneName, Is.EqualTo("Not Heating"));
        }

        [Test]
        public async Task GetAllDevices_OrdersTimerOnDevicesFirst()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device { DeviceId = 1, ZoneName = "Timer Off", IsHeating = false, IsPreheating = false, TimerOn = false, IsThermostat = true },
                new Device { DeviceId = 2, ZoneName = "Timer On", IsHeating = false, IsPreheating = false, TimerOn = true, IsThermostat = true }
            };

            _mockHeatingService.Setup(h => h.GetDevices(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(devices);

            // act
            var result = await _controller.GetAllDevices(false);

            // assert
            var resultList = result.ToList();
            Assert.That(resultList[0].ZoneName, Is.EqualTo("Timer On"));
            Assert.That(resultList[1].ZoneName, Is.EqualTo("Timer Off"));
        }

        [Test]
        public async Task GetAllDevices_OrdersThermostatsBeforeNonThermostats()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device { DeviceId = 1, ZoneName = "Timer", IsHeating = false, IsPreheating = false, TimerOn = false, IsThermostat = false },
                new Device { DeviceId = 2, ZoneName = "Thermostat", IsHeating = false, IsPreheating = false, TimerOn = false, IsThermostat = true }
            };

            _mockHeatingService.Setup(h => h.GetDevices(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(devices);

            // act
            var result = await _controller.GetAllDevices(false);

            // assert
            var resultList = result.ToList();
            Assert.That(resultList[0].ZoneName, Is.EqualTo("Thermostat"));
            Assert.That(resultList[1].ZoneName, Is.EqualTo("Timer"));
        }

        [Test]
        public async Task GetAllDevices_WithComplexOrdering_OrdersCorrectly()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device { DeviceId = 1, ZoneName = "Non-Thermostat Not Heating", IsHeating = false, IsPreheating = false, TimerOn = false, IsThermostat = false },
                new Device { DeviceId = 2, ZoneName = "Thermostat Not Heating", IsHeating = false, IsPreheating = false, TimerOn = false, IsThermostat = true },
                new Device { DeviceId = 3, ZoneName = "Non-Thermostat Heating", IsHeating = true, IsPreheating = false, TimerOn = false, IsThermostat = false },
                new Device { DeviceId = 4, ZoneName = "Thermostat Heating", IsHeating = true, IsPreheating = false, TimerOn = false, IsThermostat = true }
            };

            _mockHeatingService.Setup(h => h.GetDevices(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(devices);

            // act
            var result = await _controller.GetAllDevices(false);

            // assert
            var resultList = result.ToList();
            // First should be heating devices, ordered by thermostat first
            Assert.That(resultList[0].IsHeating || resultList[0].IsPreheating || resultList[0].TimerOn, Is.True);
            Assert.That(resultList[1].IsHeating || resultList[1].IsPreheating || resultList[1].TimerOn, Is.True);
            Assert.That(resultList[0].IsThermostat, Is.True); // Thermostat Heating
            Assert.That(resultList[1].IsThermostat, Is.False); // Non-Thermostat Heating
        }

        [Test]
        public async Task GetAllDevices_WithEmptyList_ReturnsEmptyList()
        {
            // arrange
            _mockHeatingService.Setup(h => h.GetDevices(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Device>());

            // act
            var result = await _controller.GetAllDevices(false);

            // assert
            Assert.That(result.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task GetAllDevices_WhenServiceThrows_PropagatesException()
        {
            // arrange
            _mockHeatingService.Setup(h => h.GetDevices(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Service error"));

            // act & assert
            Assert.ThrowsAsync<Exception>(async () => await _controller.GetAllDevices(false));
        }

        #endregion

        #region SetTemperature Tests

        [Test]
        public async Task SetTemperature_WithValidDevice_CallsServiceWithCorrectParameters()
        {
            // arrange
            var device = new Device
            {
                DeviceId = 1,
                ZoneName = "Living Room",
                SetTemp = 21.5
            };

            _mockHeatingService.Setup(h => h.SetTemperature(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            await _controller.SetTemperature(device);

            // assert
            _mockHeatingService.Verify(h => h.SetTemperature("Living Room", 21.5, CancellationToken.None), Times.Once);
        }

        [Test]
        public async Task SetTemperature_ReturnsOkResult()
        {
            // arrange
            var device = new Device
            {
                DeviceId = 1,
                ZoneName = "Living Room",
                SetTemp = 21.5
            };

            _mockHeatingService.Setup(h => h.SetTemperature(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await _controller.SetTemperature(device);

            // assert
            Assert.That(result, Is.InstanceOf<OkResult>());
            var okResult = result as OkResult;
            Assert.That(okResult.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task SetTemperature_WithNegativeTemperature_CallsService()
        {
            // arrange
            var device = new Device
            {
                DeviceId = 1,
                ZoneName = "Freezer",
                SetTemp = -5.0
            };

            _mockHeatingService.Setup(h => h.SetTemperature(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            await _controller.SetTemperature(device);

            // assert
            _mockHeatingService.Verify(h => h.SetTemperature("Freezer", -5.0, CancellationToken.None), Times.Once);
        }

        [Test]
        public async Task SetTemperature_WithZeroTemperature_CallsService()
        {
            // arrange
            var device = new Device
            {
                DeviceId = 1,
                ZoneName = "Living Room",
                SetTemp = 0.0
            };

            _mockHeatingService.Setup(h => h.SetTemperature(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            await _controller.SetTemperature(device);

            // assert
            _mockHeatingService.Verify(h => h.SetTemperature("Living Room", 0.0, CancellationToken.None), Times.Once);
        }

        [Test]
        public async Task SetTemperature_WithSpecialCharactersInZoneName_CallsService()
        {
            // arrange
            var device = new Device
            {
                DeviceId = 1,
                ZoneName = "Kid's Room #1",
                SetTemp = 20.0
            };

            _mockHeatingService.Setup(h => h.SetTemperature(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            await _controller.SetTemperature(device);

            // assert
            _mockHeatingService.Verify(h => h.SetTemperature("Kid's Room #1", 20.0, CancellationToken.None), Times.Once);
        }

        [Test]
        public async Task SetTemperature_WhenServiceThrows_PropagatesException()
        {
            // arrange
            var device = new Device
            {
                DeviceId = 1,
                ZoneName = "Living Room",
                SetTemp = 21.5
            };

            _mockHeatingService.Setup(h => h.SetTemperature(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Service error"));

            // act & assert
            Assert.ThrowsAsync<Exception>(async () => await _controller.SetTemperature(device));
        }

        #endregion

        #region GetHistory Tests

        [Test]
        public async Task GetHistory_WithSpecificDate_CallsServiceWithThatDate()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15);
            var history = new List<DeviceHistory>
            {
                new DeviceHistory { DeviceName = "Living Room" }
            };

            _mockHeatingService.Setup(h => h.GetDeviceHistory(testDate))
                .ReturnsAsync(history);

            // act
            var result = await _controller.GetHistory(testDate);

            // assert
            _mockHeatingService.Verify(h => h.GetDeviceHistory(testDate), Times.Once);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetHistory_WithNullDate_UsesTodayAsDefault()
        {
            // arrange
            var today = DateTime.Today;
            var history = new List<DeviceHistory>
            {
                new DeviceHistory { DeviceName = "Living Room" }
            };

            _mockHeatingService.Setup(h => h.GetDeviceHistory(It.IsAny<DateTime>()))
                .ReturnsAsync(history);

            // act
            var result = await _controller.GetHistory(null);

            // assert
            _mockHeatingService.Verify(h => h.GetDeviceHistory(
                It.Is<DateTime>(d => d.Date == today)), Times.Once);
        }

        [Test]
        public async Task GetHistory_ReturnsHistoryFromService()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15);
            var history = new List<DeviceHistory>
            {
                new DeviceHistory { DeviceName = "Living Room" },
                new DeviceHistory { DeviceName = "Bedroom" },
                new DeviceHistory { DeviceName = "Kitchen" }
            };

            _mockHeatingService.Setup(h => h.GetDeviceHistory(testDate))
                .ReturnsAsync(history);

            // act
            var result = await _controller.GetHistory(testDate);

            // assert
            var resultList = result.ToList();
            Assert.That(resultList.Count, Is.EqualTo(3));
            Assert.That(resultList[0].DeviceName, Is.EqualTo("Living Room"));
            Assert.That(resultList[1].DeviceName, Is.EqualTo("Bedroom"));
            Assert.That(resultList[2].DeviceName, Is.EqualTo("Kitchen"));
        }

        [Test]
        public async Task GetHistory_WithEmptyHistory_ReturnsEmptyList()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15);
            _mockHeatingService.Setup(h => h.GetDeviceHistory(testDate))
                .ReturnsAsync(new List<DeviceHistory>());

            // act
            var result = await _controller.GetHistory(testDate);

            // assert
            Assert.That(result.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task GetHistory_WithPastDate_CallsService()
        {
            // arrange
            var pastDate = new DateTime(2020, 1, 1);
            var history = new List<DeviceHistory>();

            _mockHeatingService.Setup(h => h.GetDeviceHistory(pastDate))
                .ReturnsAsync(history);

            // act
            var result = await _controller.GetHistory(pastDate);

            // assert
            _mockHeatingService.Verify(h => h.GetDeviceHistory(pastDate), Times.Once);
        }

        [Test]
        public async Task GetHistory_WithFutureDate_CallsService()
        {
            // arrange
            var futureDate = DateTime.Today.AddDays(30);
            var history = new List<DeviceHistory>();

            _mockHeatingService.Setup(h => h.GetDeviceHistory(futureDate))
                .ReturnsAsync(history);

            // act
            var result = await _controller.GetHistory(futureDate);

            // assert
            _mockHeatingService.Verify(h => h.GetDeviceHistory(futureDate), Times.Once);
        }

        [Test]
        public async Task GetHistory_WhenServiceThrows_PropagatesException()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15);
            _mockHeatingService.Setup(h => h.GetDeviceHistory(testDate))
                .ThrowsAsync(new Exception("Service error"));

            // act & assert
            Assert.ThrowsAsync<Exception>(async () => await _controller.GetHistory(testDate));
        }

        #endregion

        #region Setup Tests

        [Test]
        public async Task Setup_CallsRefreshDeviceList()
        {
            // arrange
            _mockHeatingService.Setup(h => h.RefreshDeviceList(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            await _controller.Setup();

            // assert
            _mockHeatingService.Verify(h => h.RefreshDeviceList(CancellationToken.None), Times.Once);
        }

        [Test]
        public async Task Setup_ReturnsOkResult()
        {
            // arrange
            _mockHeatingService.Setup(h => h.RefreshDeviceList(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await _controller.Setup();

            // assert
            Assert.That(result, Is.InstanceOf<OkResult>());
            var okResult = result as OkResult;
            Assert.That(okResult.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task Setup_WhenServiceThrows_PropagatesException()
        {
            // arrange
            _mockHeatingService.Setup(h => h.RefreshDeviceList(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Service error"));

            // act & assert
            Assert.ThrowsAsync<Exception>(async () => await _controller.Setup());
        }

        #endregion

        #region Edge Cases

        [Test]
        public async Task GetAllDevices_CalledMultipleTimes_ExecutesEachTime()
        {
            // arrange
            var devices = new List<Device>
            {
                new Device { DeviceId = 1, ZoneName = "Living Room", IsThermostat = true }
            };

            _mockHeatingService.Setup(h => h.GetDevices(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(devices);

            // act
            await _controller.GetAllDevices(false);
            await _controller.GetAllDevices(true);
            await _controller.GetAllDevices(false);

            // assert
            _mockHeatingService.Verify(h => h.GetDevices(It.IsAny<bool>(), CancellationToken.None), Times.Exactly(3));
        }

        [Test]
        public async Task SetTemperature_CalledMultipleTimes_ExecutesEachTime()
        {
            // arrange
            var device = new Device { DeviceId = 1, ZoneName = "Living Room", SetTemp = 20.0 };

            _mockHeatingService.Setup(h => h.SetTemperature(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            await _controller.SetTemperature(device);
            await _controller.SetTemperature(device);

            // assert
            _mockHeatingService.Verify(h => h.SetTemperature("Living Room", 20.0, CancellationToken.None), Times.Exactly(2));
        }

        [Test]
        public async Task GetHistory_WithDateTimeMinValue_CallsService()
        {
            // arrange
            var minDate = DateTime.MinValue;
            _mockHeatingService.Setup(h => h.GetDeviceHistory(minDate))
                .ReturnsAsync(new List<DeviceHistory>());

            // act
            var result = await _controller.GetHistory(minDate);

            // assert
            _mockHeatingService.Verify(h => h.GetDeviceHistory(minDate), Times.Once);
        }

        [Test]
        public async Task GetHistory_WithDateTimeMaxValue_CallsService()
        {
            // arrange
            var maxDate = DateTime.MaxValue;
            _mockHeatingService.Setup(h => h.GetDeviceHistory(maxDate))
                .ReturnsAsync(new List<DeviceHistory>());

            // act
            var result = await _controller.GetHistory(maxDate);

            // assert
            _mockHeatingService.Verify(h => h.GetDeviceHistory(maxDate), Times.Once);
        }

        #endregion
    }
}