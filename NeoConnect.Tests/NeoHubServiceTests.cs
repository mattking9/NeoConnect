using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NeoConnect.Tests
{
    public class NeoHubServiceTests
    {
        private readonly Mock<ILogger<NeoHubService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ClientWebSocketWrapper> _mockWs;
        private readonly NeoHubService _service;
        private readonly CancellationToken _token;

        public NeoHubServiceTests()
        {
            _mockLogger = new Mock<ILogger<NeoHubService>>();
            _mockConfig = new Mock<IConfiguration>();
            _mockWs = new Mock<ClientWebSocketWrapper>();
            
            // Setup configuration
            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(x => x.Value).Returns("test-api-key");
            _mockConfig.Setup(x => x.GetSection("NeoHub:ApiKey")).Returns(mockConfigSection.Object);
            _mockConfig.Setup(x => x.GetValue<string>("NeoHub:ApiKey")).Returns("test-api-key");
            _mockConfig.Setup(x => x.GetValue<Uri>("NeoHub:Uri")).Returns(new Uri("ws://localhost:4242"));
            
            // Setup WebSocket
            _mockWs.Setup(x => x.State).Returns(WebSocketState.Open);
            
            _service = new NeoHubService(_mockLogger.Object, _mockConfig.Object, _mockWs.Object);
            _token = CancellationToken.None;
        }

        [Fact]
        public async Task GetEngineersData_ShouldReturnDictionary_WhenSuccessful()
        {
            // Arrange
            var engineersData = new Dictionary<string, EngineersData>
            {
                ["Living Room"] = new EngineersData { DeviceId = 1, DeviceType = 2, MaxPreheatDuration = 3 },
                ["Bedroom"] = new EngineersData { DeviceId = 4, DeviceType = 5, MaxPreheatDuration = 6 }
            };
            
            var responseJson = JsonSerializer.Serialize(engineersData);
            var response = new NeoHubResponse { ResponseJson = responseJson };
            var responseWrapper = JsonSerializer.Serialize(response);
            
            _mockWs.Setup(x => x.ReceiveAllAsync(_token)).ReturnsAsync(responseWrapper);

            // Act
            var result = await _service.GetEngineersData(_token);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(3, result["Living Room"].MaxPreheatDuration);
            Assert.Equal(6, result["Bedroom"].MaxPreheatDuration);
            
            // Verify that the correct command was sent
            _mockWs.Verify(x => x.SendAllAsync(It.Is<string>(s => 
                s.Contains("\"COMMAND\":\"{'GET_ENGINEERS':0}\"") && s.Contains("\"COMMANDID\":3")), _token), Times.Once);
        }

        [Fact]
        public async Task GetAllProfiles_ShouldReturnDictionary_WhenSuccessful()
        {
            // Arrange
            var profiles = new Dictionary<string, Profile>
            {
                ["Profile1"] = new Profile { ProfileId = 1, ProfileName = "Weekday", Schedule = new ProfileSchedule() },
                ["Profile2"] = new Profile { ProfileId = 2, ProfileName = "Weekend", Schedule = new ProfileSchedule() }
            };
            
            var responseJson = JsonSerializer.Serialize(profiles);
            var response = new NeoHubResponse { ResponseJson = responseJson };
            var responseWrapper = JsonSerializer.Serialize(response);
            
            _mockWs.Setup(x => x.ReceiveAllAsync(_token)).ReturnsAsync(responseWrapper);

            // Act
            var result = await _service.GetAllProfiles(_token);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Weekday", result[1].ProfileName);
            Assert.Equal("Weekend", result[2].ProfileName);
            
            // Verify that the correct command was sent
            _mockWs.Verify(x => x.SendAllAsync(It.Is<string>(s => 
                s.Contains("\"COMMAND\":\"{'GET_PROFILES':0}\"") && s.Contains("\"COMMANDID\":2")), _token), Times.Once);
        }

        [Fact]
        public async Task GetROCData_ShouldReturnDictionary_WhenSuccessful()
        {
            // Arrange
            var devices = new[] { "Living Room", "Bedroom" };
            var rocData = new Dictionary<string, int>
            {
                ["Living Room"] = 10,
                ["Bedroom"] = 15
            };
            
            var responseJson = JsonSerializer.Serialize(rocData);
            var response = new NeoHubResponse { ResponseJson = responseJson };
            var responseWrapper = JsonSerializer.Serialize(response);
            
            _mockWs.Setup(x => x.ReceiveAllAsync(_token)).ReturnsAsync(responseWrapper);

            // Act
            var result = await _service.GetROCData(devices, _token);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(10, result["Living Room"]);
            Assert.Equal(15, result["Bedroom"]);
            
            // Verify that the correct command was sent with the device names
            _mockWs.Verify(x => x.SendAllAsync(It.Is<string>(s => 
                s.Contains("\"COMMAND\":\"{'VIEW_ROC':['Living Room','Bedroom']}\"") && 
                s.Contains("\"COMMANDID\":5")), _token), Times.Once);
        }

        [Fact]
        public async Task RunRecipe_ShouldSendCorrectCommand_WhenCalled()
        {
            // Arrange
            var recipeName = "Test Recipe";
            var response = new NeoHubResponse();
            var responseWrapper = JsonSerializer.Serialize(response);
            
            _mockWs.Setup(x => x.ReceiveAllAsync(_token)).ReturnsAsync(responseWrapper);

            // Act
            await _service.RunRecipe(recipeName, _token);

            // Assert
            // Verify that the correct command was sent with the recipe name
            _mockWs.Verify(x => x.SendAllAsync(It.Is<string>(s => 
                s.Contains($"\"COMMAND\":\"{'RUN_RECIPE':['Test Recipe']}\"") && 
                s.Contains("\"COMMANDID\":4")), _token), Times.Once);
        }

        [Fact]
        public async Task SetPreheatDuration_ShouldSendCorrectCommand_WhenCalled()
        {
            // Arrange
            var zoneName = "Living Room";
            var duration = 2;
            var response = new NeoHubResponse();
            var responseWrapper = JsonSerializer.Serialize(response);
            
            _mockWs.Setup(x => x.ReceiveAllAsync(_token)).ReturnsAsync(responseWrapper);

            // Act
            await _service.SetPreheatDuration(zoneName, duration, _token);

            // Assert
            // Verify that the correct command was sent with zone name and duration
            _mockWs.Verify(x => x.SendAllAsync(It.Is<string>(s => 
                s.Contains($"\"COMMAND\":\"{'SET_PREHEAT'}:[2, 'Living Room']\"") && 
                s.Contains("\"COMMANDID\":5")), _token), Times.Once);
        }

        [Fact]
        public void GetNextSwitchingInterval_ShouldReturnCorrectInterval_ForWeekday()
        {
            // Arrange
            var schedule = new ProfileSchedule
            {
                Weekdays = new ProfileScheduleGroup
                {
                    Wake = new object[] { "06:00", 20.0 },
                    Leave = new object[] { "08:30", 17.0 },
                    Return = new object[] { "17:00", 20.0 },
                    Sleep = new object[] { "22:00", 16.0 }
                },
                Weekends = new ProfileScheduleGroup
                {
                    Wake = new object[] { "07:00", 20.0 },
                    Leave = new object[] { "10:00", 18.0 },
                    Return = new object[] { "15:00", 20.0 },
                    Sleep = new object[] { "23:00", 16.0 }
                }
            };

            // Testing on a Monday at 9:00
            var mondayAt9 = new DateTime(2023, 5, 1, 9, 0, 0); // May 1, 2023 was a Monday

            // Act
            var result = _service.GetNextSwitchingInterval(schedule, mondayAt9);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(17, TimeOnly.FromTimeSpan(result.Time.ToTimeSpan()).Hour);
            Assert.Equal(0, TimeOnly.FromTimeSpan(result.Time.ToTimeSpan()).Minute);
            Assert.Equal(20.0, result.TargetTemp);
        }

        [Fact]
        public void GetNextSwitchingInterval_ShouldReturnCorrectInterval_ForWeekend()
        {
            // Arrange
            var schedule = new ProfileSchedule
            {
                Weekdays = new ProfileScheduleGroup
                {
                    Wake = new object[] { "06:00", 20.0 },
                    Leave = new object[] { "08:30", 17.0 },
                    Return = new object[] { "17:00", 20.0 },
                    Sleep = new object[] { "22:00", 16.0 }
                },
                Weekends = new ProfileScheduleGroup
                {
                    Wake = new object[] { "07:00", 20.0 },
                    Leave = new object[] { "10:00", 18.0 },
                    Return = new object[] { "15:00", 20.0 },
                    Sleep = new object[] { "23:00", 16.0 }
                }
            };

            // Testing on a Saturday at 9:00
            var saturdayAt9 = new DateTime(2023, 5, 6, 9, 0, 0); // May 6, 2023 was a Saturday

            // Act
            var result = _service.GetNextSwitchingInterval(schedule, saturdayAt9);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, TimeOnly.FromTimeSpan(result.Time.ToTimeSpan()).Hour);
            Assert.Equal(0, TimeOnly.FromTimeSpan(result.Time.ToTimeSpan()).Minute);
            Assert.Equal(18.0, result.TargetTemp);
        }
    }
}