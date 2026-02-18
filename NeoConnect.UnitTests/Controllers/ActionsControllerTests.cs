using Microsoft.AspNetCore.Mvc;
using Moq;

namespace NeoConnect.UnitTests.Controllers
{
    [TestFixture]
    public class ActionsControllerTests
    {
        private Mock<IHeatingService> _mockHeatingService;
        private Mock<IWeatherService> _mockWeatherService;
        private ActionsController _controller;
        private CancellationTokenSource _cts;

        [SetUp]
        public void Setup()
        {
            _mockHeatingService = new Mock<IHeatingService>();
            _mockWeatherService = new Mock<IWeatherService>();
            _controller = new ActionsController(_mockHeatingService.Object, _mockWeatherService.Object);
            _cts = new CancellationTokenSource();
        }

        [TearDown]
        public void TearDown()
        {
            _cts.Dispose();
        }        

        #region Bathroom Boost Action Tests

        [Test]
        public async Task Post_WithBathroomBoostAction_CallsBoostTowelRailMethod()
        {
            // arrange
            _mockHeatingService.Setup(h => h.BoostTowelRailWhenBathroomIsCold(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await _controller.Post("bathroom_boost");

            // assert
            _mockHeatingService.Verify(h => h.BoostTowelRailWhenBathroomIsCold(CancellationToken.None), Times.Once);
            Assert.That(result, Is.InstanceOf<OkResult>());
        }

        [Test]
        public async Task Post_WithBathroomBoostAction_ReturnsOkResult()
        {
            // arrange
            _mockHeatingService.Setup(h => h.BoostTowelRailWhenBathroomIsCold(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await _controller.Post("bathroom_boost");

            // assert
            Assert.That(result, Is.InstanceOf<OkResult>());
            var okResult = result as OkResult;
            Assert.That(okResult.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task Post_WithBathroomBoostAction_WhenServiceThrows_PropagatesException()
        {
            // arrange
            _mockHeatingService.Setup(h => h.BoostTowelRailWhenBathroomIsCold(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Service error"));

            // act & assert
            Assert.ThrowsAsync<Exception>(async () => await _controller.Post("bathroom_boost"));
        }

        #endregion

        #region Global Hold Action Tests

        [Test]
        public async Task Post_WithGlobalHoldAction_CallsWeatherServiceAndHeatingService()
        {
            // arrange
            var forecast = new Forecast
            {
                ForecastDay = new List<ForecastDay>
                    {
                        new ForecastDay
                        {
                            Hour = Enumerable.Range(0, 24).Select(h => new ForecastHour
                            {
                                Temp = 15,
                                Condition = new ForecastCondition { Text = "Sunny" }
                            }).ToList()
                        }
                    }
            };

            _mockWeatherService.Setup(w => w.GetForecast(It.IsAny<CancellationToken>()))
                .ReturnsAsync(forecast);
            _mockHeatingService.Setup(h => h.ReduceSetTempWhenExternalTempIsWarm(It.IsAny<ForecastDay>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await _controller.Post("global_hold");

            // assert
            _mockWeatherService.Verify(w => w.GetForecast(CancellationToken.None), Times.Once);
            _mockHeatingService.Verify(h => h.ReduceSetTempWhenExternalTempIsWarm(
                It.Is<ForecastDay>(f => f.Hour.Count == 24), 
                CancellationToken.None), Times.Once);
            Assert.That(result, Is.InstanceOf<OkResult>());
        }

        [Test]
        public async Task Post_WithGlobalHoldAction_ReturnsOkResult()
        {
            // arrange
            var forecast = new Forecast
            {
                ForecastDay = new List<ForecastDay>
                    {
                        new ForecastDay
                        {
                            Hour = new List<ForecastHour>()
                        }
                    }
            };

            _mockWeatherService.Setup(w => w.GetForecast(It.IsAny<CancellationToken>()))
                .ReturnsAsync(forecast);
            _mockHeatingService.Setup(h => h.ReduceSetTempWhenExternalTempIsWarm(It.IsAny<ForecastDay>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await _controller.Post("global_hold");

            // assert
            Assert.That(result, Is.InstanceOf<OkResult>());
            var okResult = result as OkResult;
            Assert.That(okResult.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task Post_WithGlobalHoldAction_PassesFirstForecastDayToHeatingService()
        {
            // arrange
            var firstDay = new ForecastDay
            {
                Hour = Enumerable.Range(0, 24).Select(h => new ForecastHour
                {
                    Temp = 20,
                    Condition = new ForecastCondition { Text = "Cloudy" }
                }).ToList()
            };

            var secondDay = new ForecastDay
            {
                Hour = Enumerable.Range(0, 24).Select(h => new ForecastHour
                {
                    Temp = 10,
                    Condition = new ForecastCondition { Text = "Rainy" }
                }).ToList()
            };

            var forecast = new Forecast
            {
                ForecastDay = new List<ForecastDay> { firstDay, secondDay }
            };

            ForecastDay capturedForecastDay = null;
            _mockWeatherService.Setup(w => w.GetForecast(It.IsAny<CancellationToken>()))
                .ReturnsAsync(forecast);
            _mockHeatingService.Setup(h => h.ReduceSetTempWhenExternalTempIsWarm(It.IsAny<ForecastDay>(), It.IsAny<CancellationToken>()))
                .Callback<ForecastDay, CancellationToken>((fd, ct) => capturedForecastDay = fd)
                .Returns(Task.CompletedTask);

            // act
            await _controller.Post("global_hold");

            // assert
            Assert.That(capturedForecastDay, Is.Not.Null);
            Assert.That(capturedForecastDay, Is.SameAs(firstDay));
            Assert.That(capturedForecastDay.Hour[0].Temp, Is.EqualTo(20));
        }

        [Test]
        public async Task Post_WithGlobalHoldAction_WhenWeatherServiceThrows_PropagatesException()
        {
            // arrange
            _mockWeatherService.Setup(w => w.GetForecast(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Weather service error"));

            // act & assert
            Assert.ThrowsAsync<Exception>(async () => await _controller.Post("global_hold"));
            
            // Verify heating service was never called
            _mockHeatingService.Verify(h => h.ReduceSetTempWhenExternalTempIsWarm(
                It.IsAny<ForecastDay>(), 
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Post_WithGlobalHoldAction_WhenHeatingServiceThrows_PropagatesException()
        {
            // arrange
            var forecast = new Forecast
            {
                ForecastDay = new List<ForecastDay>
                    {
                        new ForecastDay
                        {
                            Hour = new List<ForecastHour>()
                        }
                    }
            };

            _mockWeatherService.Setup(w => w.GetForecast(It.IsAny<CancellationToken>()))
                .ReturnsAsync(forecast);
            _mockHeatingService.Setup(h => h.ReduceSetTempWhenExternalTempIsWarm(It.IsAny<ForecastDay>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Heating service error"));

            // act & assert
            Assert.ThrowsAsync<Exception>(async () => await _controller.Post("global_hold"));
        }

        #endregion

        #region Data Collection Action Tests

        [Test]
        public async Task Post_WithDataCollectionAction_CallsLogDeviceStatusesMethod()
        {
            // arrange
            _mockHeatingService.Setup(h => h.LogDeviceStatuses(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await _controller.Post("data_collection");

            // assert
            _mockHeatingService.Verify(h => h.LogDeviceStatuses(CancellationToken.None), Times.Once);
            Assert.That(result, Is.InstanceOf<OkResult>());
        }

        [Test]
        public async Task Post_WithDataCollectionAction_ReturnsOkResult()
        {
            // arrange
            _mockHeatingService.Setup(h => h.LogDeviceStatuses(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await _controller.Post("data_collection");

            // assert
            Assert.That(result, Is.InstanceOf<OkResult>());
            var okResult = result as OkResult;
            Assert.That(okResult.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task Post_WithDataCollectionAction_WhenServiceThrows_PropagatesException()
        {
            // arrange
            _mockHeatingService.Setup(h => h.LogDeviceStatuses(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Service error"));

            // act & assert
            Assert.ThrowsAsync<Exception>(async () => await _controller.Post("data_collection"));
        }

        #endregion

        #region Invalid Action Tests

        [Test]
        public async Task Post_WithInvalidActionName_ReturnsBadRequest()
        {
            // act
            var result = await _controller.Post("invalid_action");

            // assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
        }

        [Test]
        public async Task Post_WithInvalidActionName_ReturnsErrorMessageWithValidActions()
        {
            // act
            var result = await _controller.Post("invalid_action");

            // assert
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Is.Not.Null);
            var errorMessage = badRequestResult.Value.ToString();
            Assert.That(errorMessage, Does.Contain("Invalid action name"));
            Assert.That(errorMessage, Does.Contain("bathroom_boost"));
            Assert.That(errorMessage, Does.Contain("global_hold"));
            Assert.That(errorMessage, Does.Contain("data_collection"));
        }

        [Test]
        public async Task Post_WithEmptyActionName_ReturnsBadRequest()
        {
            // act
            var result = await _controller.Post("");

            // assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Post_WithNullActionName_ReturnsBadRequest()
        {
            // act
            var result = await _controller.Post(null);

            // assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Post_WithWhitespaceActionName_ReturnsBadRequest()
        {
            // act
            var result = await _controller.Post("   ");

            // assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Post_WithInvalidActionName_DoesNotCallAnyService()
        {
            // act
            await _controller.Post("invalid_action");

            // assert
            _mockHeatingService.Verify(h => h.BoostTowelRailWhenBathroomIsCold(It.IsAny<CancellationToken>()), Times.Never);
            _mockHeatingService.Verify(h => h.ReduceSetTempWhenExternalTempIsWarm(It.IsAny<ForecastDay>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockHeatingService.Verify(h => h.LogDeviceStatuses(It.IsAny<CancellationToken>()), Times.Never);
            _mockWeatherService.Verify(w => w.GetForecast(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Case Sensitivity Tests

        [Test]
        public async Task Post_WithUpperCaseActionName_ReturnsBadRequest()
        {
            // act
            var result = await _controller.Post("BATHROOM_BOOST");

            // assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Post_WithMixedCaseActionName_ReturnsBadRequest()
        {
            // act
            var result = await _controller.Post("Bathroom_Boost");

            // assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Post_WithCorrectLowerCaseActionName_ReturnsOk()
        {
            // arrange
            _mockHeatingService.Setup(h => h.BoostTowelRailWhenBathroomIsCold(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await _controller.Post("bathroom_boost");

            // assert
            Assert.That(result, Is.InstanceOf<OkResult>());
        }

        #endregion

        #region All Valid Actions Tests

        [Test]
        [TestCase("bathroom_boost")]
        [TestCase("global_hold")]
        [TestCase("data_collection")]
        public async Task Post_WithAllValidActions_ReturnsOkResult(string actionName)
        {
            // arrange
            var forecast = new Forecast
            {
                ForecastDay = new List<ForecastDay>
                    {
                        new ForecastDay
                        {
                            Hour = new List<ForecastHour>()
                        }
                    }
            };

            _mockHeatingService.Setup(h => h.BoostTowelRailWhenBathroomIsCold(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockWeatherService.Setup(w => w.GetForecast(It.IsAny<CancellationToken>()))
                .ReturnsAsync(forecast);
            _mockHeatingService.Setup(h => h.ReduceSetTempWhenExternalTempIsWarm(It.IsAny<ForecastDay>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockHeatingService.Setup(h => h.LogDeviceStatuses(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await _controller.Post(actionName);

            // assert
            Assert.That(result, Is.InstanceOf<OkResult>());
        }

        #endregion

        #region Edge Cases

        [Test]
        public async Task Post_WithActionNameWithLeadingSpaces_ReturnsBadRequest()
        {
            // act
            var result = await _controller.Post("  bathroom_boost");

            // assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Post_WithActionNameWithTrailingSpaces_ReturnsBadRequest()
        {
            // act
            var result = await _controller.Post("bathroom_boost  ");

            // assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Post_CalledMultipleTimes_ExecutesEachTime()
        {
            // arrange
            _mockHeatingService.Setup(h => h.LogDeviceStatuses(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            await _controller.Post("data_collection");
            await _controller.Post("data_collection");
            await _controller.Post("data_collection");

            // assert
            _mockHeatingService.Verify(h => h.LogDeviceStatuses(CancellationToken.None), Times.Exactly(3));
        }

        [Test]
        public async Task Post_WithDifferentActionsInSequence_CallsCorrectServices()
        {
            // arrange
            var forecast = new Forecast
            {
                ForecastDay = new List<ForecastDay>
                    {
                        new ForecastDay
                        {
                            Hour = new List<ForecastHour>()
                        }
                    }
            };

            _mockHeatingService.Setup(h => h.BoostTowelRailWhenBathroomIsCold(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockWeatherService.Setup(w => w.GetForecast(It.IsAny<CancellationToken>()))
                .ReturnsAsync(forecast);
            _mockHeatingService.Setup(h => h.ReduceSetTempWhenExternalTempIsWarm(It.IsAny<ForecastDay>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockHeatingService.Setup(h => h.LogDeviceStatuses(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            await _controller.Post("bathroom_boost");
            await _controller.Post("global_hold");
            await _controller.Post("data_collection");

            // assert
            _mockHeatingService.Verify(h => h.BoostTowelRailWhenBathroomIsCold(CancellationToken.None), Times.Once);
            _mockWeatherService.Verify(w => w.GetForecast(CancellationToken.None), Times.Once);
            _mockHeatingService.Verify(h => h.ReduceSetTempWhenExternalTempIsWarm(It.IsAny<ForecastDay>(), CancellationToken.None), Times.Once);
            _mockHeatingService.Verify(h => h.LogDeviceStatuses(CancellationToken.None), Times.Once);
        }

        #endregion
    }
}