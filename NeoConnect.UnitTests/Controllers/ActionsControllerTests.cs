using Microsoft.AspNetCore.Mvc;
using Moq;

namespace NeoConnect.UnitTests.Controllers
{
    [TestFixture]
    public class ActionsControllerTests
    {
        private Mock<IScheduledAction> _mockAction1;
        private Mock<IScheduledAction> _mockAction2;
        private ActionsController _controller;
        private CancellationTokenSource _cts;

        [SetUp]
        public void Setup()
        {
            _mockAction1 = new Mock<IScheduledAction>();
            _mockAction1.Setup(a => a.Id).Returns("test_action_1");
            _mockAction1.Setup(a => a.Name).Returns("Test Action 1");
            _mockAction1.Setup(a => a.Action(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _mockAction2 = new Mock<IScheduledAction>();
            _mockAction2.Setup(a => a.Id).Returns("test_action_2");
            _mockAction2.Setup(a => a.Name).Returns("Test Action 2");
            _mockAction2.Setup(a => a.Action(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var actions = new List<IScheduledAction> { _mockAction1.Object, _mockAction2.Object };

            _controller = new ActionsController(actions);
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
            // arrange
            var actions = new List<IScheduledAction> { _mockAction1.Object };

            // act
            var controller = new ActionsController(actions);

            // assert
            Assert.That(controller, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithEmptyActionsList_CreatesInstance()
        {
            // arrange
            var actions = new List<IScheduledAction>();

            // act
            var controller = new ActionsController(actions);

            // assert
            Assert.That(controller, Is.Not.Null);
        }

        #endregion

        #region Post Tests

        [Test]
        public async Task Post_WithValidActionName_ReturnsOkResult()
        {
            // arrange
            var actionName = "test_action_1";

            // act
            var result = await _controller.Post(actionName);

            // assert
            Assert.That(result, Is.InstanceOf<OkResult>());
        }

        [Test]
        public async Task Post_WithValidActionName_CallsActionMethod()
        {
            // arrange
            var actionName = "test_action_1";

            // act
            await _controller.Post(actionName);

            // assert
            _mockAction1.Verify(a => a.Action(CancellationToken.None), Times.Once);
        }

        [Test]
        public async Task Post_WithValidActionName_CallsCorrectAction()
        {
            // arrange
            var actionName = "test_action_2";

            // act
            await _controller.Post(actionName);

            // assert
            _mockAction2.Verify(a => a.Action(CancellationToken.None), Times.Once);
            _mockAction1.Verify(a => a.Action(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Post_WithInvalidActionName_ReturnsBadRequest()
        {
            // arrange
            var actionName = "non_existent_action";

            // act
            var result = await _controller.Post(actionName);

            // assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Post_WithInvalidActionName_ReturnsCorrectErrorMessage()
        {
            // arrange
            var actionName = "non_existent_action";

            // act
            var result = await _controller.Post(actionName) as BadRequestObjectResult;

            // assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Value, Is.EqualTo("Invalid action name."));
        }

        [Test]
        public async Task Post_WithInvalidActionName_DoesNotCallAnyAction()
        {
            // arrange
            var actionName = "non_existent_action";

            // act
            await _controller.Post(actionName);

            // assert
            _mockAction1.Verify(a => a.Action(It.IsAny<CancellationToken>()), Times.Never);
            _mockAction2.Verify(a => a.Action(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Post_WithNullActionName_ReturnsBadRequest()
        {
            // arrange
            string actionName = null;

            // act
            var result = await _controller.Post(actionName);

            // assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Post_WithEmptyActionName_ReturnsBadRequest()
        {
            // arrange
            var actionName = string.Empty;

            // act
            var result = await _controller.Post(actionName);

            // assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Post_ActionThrowsException_PropagatesException()
        {
            // arrange
            var actionName = "test_action_1";
            _mockAction1.Setup(a => a.Action(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            // act & assert
            Assert.ThrowsAsync<Exception>(async () => await _controller.Post(actionName));
        }

        [Test]
        public async Task Post_WithMultipleActions_ExecutesOnlyRequestedAction()
        {
            // arrange
            var actionName = "test_action_1";

            // act
            await _controller.Post(actionName);

            // assert
            _mockAction1.Verify(a => a.Action(CancellationToken.None), Times.Once);
            _mockAction2.Verify(a => a.Action(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion
    }
}