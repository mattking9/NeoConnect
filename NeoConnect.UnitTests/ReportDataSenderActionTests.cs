using Moq;
using Microsoft.Extensions.Configuration;

namespace NeoConnect.UnitTests
{
    [TestFixture]
    public class ReportDataSenderActionTests
    {
        private Mock<IConfiguration> _mockConfig;
        private Mock<IEmailService> _mockEmailService;
        private Mock<IReportDataService> _mockReportDataService;
        private ReportDataSenderAction _action;

        [SetUp]
        public void Setup()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockEmailService = new Mock<IEmailService>();
            _mockReportDataService = new Mock<IReportDataService>();

            _action = new ReportDataSenderAction(_mockConfig.Object, _mockEmailService.Object, _mockReportDataService.Object);
        }

        [Test]
        public void Name_ReturnsExpectedValue()
        {
            Assert.That(_action.Name, Is.EqualTo("Report Data Sender"));
        }

        [Test]
        public void Schedule_ReturnsConfigValue()
        {
            _mockConfig.Setup(c => c["ReportDataSenderSchedule"]).Returns("0 8 * * *");
            Assert.That(_action.Schedule, Is.EqualTo("0 8 * * *"));
        }

        [Test]
        public async Task Action_CallsSendEmailWithReport()
        {
            // Arrange
            var token = new CancellationToken();
            var htmlReport = "<h1>NeoConnect Report</h1>";
            _mockReportDataService.Setup(r => r.ToHtmlReportString()).Returns(htmlReport);
            _mockEmailService.Setup(e => e.SendEmail("NeoConnect Report", htmlReport, token)).Returns(Task.CompletedTask);

            // Act
            await _action.Action(token);

            // Assert
            _mockEmailService.Verify(e => e.SendEmail("NeoConnect Report", htmlReport, token), Times.Once);
        }
    }
}