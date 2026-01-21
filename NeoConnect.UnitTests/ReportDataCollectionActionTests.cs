using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NeoConnect.UnitTests
{
    [TestFixture]
    public class ReportDataCollectionActionTests
    {
        private Mock<IConfiguration> _mockConfig;
        private Mock<IServiceScopeFactory> _mockScopeFactory;
        private Mock<IServiceScope> _mockScope;
        private Mock<IServiceProvider> _mockProvider;
        private Mock<IHeatingService> _mockHeatingService;
        private ReportDataCollectionAction _action;

        [SetUp]
        public void Setup()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockScope = new Mock<IServiceScope>();
            _mockProvider = new Mock<IServiceProvider>();
            _mockHeatingService = new Mock<IHeatingService>();

            _mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockScope.Object);
            _mockScope.Setup(s => s.ServiceProvider).Returns(_mockProvider.Object);
            _mockProvider.Setup(p => p.GetService(typeof(IHeatingService))).Returns(_mockHeatingService.Object);

            _action = new ReportDataCollectionAction(_mockConfig.Object, _mockScopeFactory.Object);
        }

        [Test]
        public void Name_ReturnsExpectedValue()
        {
            Assert.That(_action.Name, Is.EqualTo("Report Data Collection"));
        }

        [Test]
        public void Schedule_ReturnsConfigValue()
        {
            _mockConfig.Setup(c => c["ReportDataCollectionSchedule"]).Returns("0 0 * * *");
            Assert.That(_action.Schedule, Is.EqualTo("0 0 * * *"));
        }

        [Test]
        public async Task Action_CallsHeatingServiceMethodsInOrder()
        {
            // Arrange
            var token = new CancellationToken();
            _mockHeatingService.Setup(s => s.Init(token)).Returns(Task.CompletedTask);
            _mockHeatingService.Setup(s => s.LogDeviceStatuses(token)).Returns(Task.CompletedTask);
            _mockHeatingService.Setup(s => s.Cleanup(token)).Returns(Task.CompletedTask);

            // Act
            await _action.Action(token);

            // Assert
            _mockHeatingService.Verify(s => s.Init(token), Times.Once);
            _mockHeatingService.Verify(s => s.LogDeviceStatuses(token), Times.Once);
            _mockHeatingService.Verify(s => s.Cleanup(token), Times.Once);
        }
    }
}