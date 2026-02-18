using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace NeoConnect.UnitTests.Services
{
    [TestFixture]
    public class EmailServiceTests
    {
        private Mock<ILogger<EmailService>> _mockLogger;
        private Mock<ISmtpClientWrapper> _mockSmtpClient;
        private IConfiguration _configuration;
        private EmailService _emailService;
        private CancellationTokenSource _cts;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<EmailService>>();
            _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            _mockSmtpClient = new Mock<ISmtpClientWrapper>();

            var inMemorySettings = new Dictionary<string, string>
            {
                {"Smtp:Host", "smtp.example.com"},
                {"Smtp:Port", "587"},
                {"Smtp:Username", "test@example.com"},
                {"Smtp:Password", "password"},
                {"Smtp:ToAddress", "recipient@example.com"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _cts = new CancellationTokenSource();

            _emailService = new EmailService(_mockLogger.Object, _configuration, _mockSmtpClient.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _cts.Dispose();
        }

        #region SendInfoEmail (string) Tests

        [Test]
        public async Task SendInfoEmail_WithSingleString_SendsEmailWithCorrectContent()
        {
            // arrange
            var info = "Test action performed";
            _mockSmtpClient.Setup(s => s.SendMailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await _emailService.SendInfoEmail(info, _cts.Token);

            // assert
            Assert.That(result, Is.True);
            _mockSmtpClient.Verify(s => s.SendMailAsync(
                "test@example.com",
                "recipient@example.com",
                "Neo Connect Made Changes",
                It.Is<string>(body => body.Contains("Test action performed")),
                true,
                _cts.Token), Times.Once);
        }

        [Test]
        public async Task SendInfoEmail_WithSingleString_LogsInformation()
        {
            // arrange
            var info = "Test action";
            _mockSmtpClient.Setup(s => s.SendMailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            await _emailService.SendInfoEmail(info, _cts.Token);

            // assert
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Sending Email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        #endregion

        #region SendInfoEmail (IEnumerable) Tests

        [Test]
        public async Task SendInfoEmail_WithMultipleItems_SendsEmailWithAllItems()
        {
            // arrange
            var items = new List<string> { "Action 1", "Action 2", "Action 3" };
            _mockSmtpClient.Setup(s => s.SendMailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await _emailService.SendInfoEmail(items, _cts.Token);

            // assert
            Assert.That(result, Is.True);
            _mockSmtpClient.Verify(s => s.SendMailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(body => 
                    body.Contains("Action 1") && 
                    body.Contains("Action 2") && 
                    body.Contains("Action 3")),
                true,
                _cts.Token), Times.Once);
        }

        [Test]
        public async Task SendInfoEmail_WithEmptyList_DoesNotSendEmail()
        {
            // arrange
            var items = new List<string>();

            // act
            var result = await _emailService.SendInfoEmail(items, _cts.Token);

            // assert
            Assert.That(result, Is.False);
            _mockSmtpClient.Verify(s => s.SendMailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No email body")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Test]
        public async Task SendInfoEmail_WithNullList_DoesNotSendEmail()
        {
            // arrange
            IEnumerable<string> items = null;

            // act
            var result = await _emailService.SendInfoEmail(items, _cts.Token);

            // assert
            Assert.That(result, Is.False);
            _mockSmtpClient.Verify(s => s.SendMailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SendInfoEmail_CreatesHtmlFormattedBody()
        {
            // arrange
            var items = new List<string> { "Test item" };
            string capturedBody = null;
            _mockSmtpClient.Setup(s => s.SendMailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, string, bool, CancellationToken>(
                    (from, to, subject, body, isHtml, ct) => capturedBody = body)
                .Returns(Task.CompletedTask);

            // act
            await _emailService.SendInfoEmail(items, _cts.Token);

            // assert
            Assert.That(capturedBody, Does.Contain("<html>"));
            Assert.That(capturedBody, Does.Contain("<ul>"));
            Assert.That(capturedBody, Does.Contain("<li>Test item</li>"));
            Assert.That(capturedBody, Does.Contain("</ul>"));
            Assert.That(capturedBody, Does.Contain("</html>"));
        }

        #endregion

        #region SendErrorEmail Tests

        [Test]
        public async Task SendErrorEmail_WithException_SendsEmailWithErrorDetails()
        {
            // arrange
            var exception = new Exception("Test error message");
            _mockSmtpClient.Setup(s => s.SendMailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await _emailService.SendErrorEmail(exception, _cts.Token);

            // assert
            Assert.That(result, Is.True);
            _mockSmtpClient.Verify(s => s.SendMailAsync(
                "test@example.com",
                "recipient@example.com",
                "Neo Connect Error",
                It.Is<string>(body => body.Contains("Test error message")),
                true,
                _cts.Token), Times.Once);
        }

        [Test]
        public async Task SendErrorEmail_WithExceptionWithStackTrace_IncludesStackTrace()
        {
            // arrange
            Exception exception;
            try
            {
                throw new InvalidOperationException("Test error");
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            _mockSmtpClient.Setup(s => s.SendMailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            await _emailService.SendErrorEmail(exception, _cts.Token);

            // assert
            _mockSmtpClient.Verify(s => s.SendMailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(body => body.Contains("Test error") && !body.Contains("Stack trace unavailable")),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SendErrorEmail_WithNullException_SendsEmailWithoutCrashing()
        {
            // arrange
            _mockSmtpClient.Setup(s => s.SendMailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            var result = await _emailService.SendErrorEmail(null, _cts.Token);

            // assert
            Assert.That(result, Is.True);
            _mockSmtpClient.Verify(s => s.SendMailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                "Neo Connect Error",
                It.Is<string>(body => body.Contains("Stack trace unavailable")),
                true,
                _cts.Token), Times.Once);
        }

        [Test]
        public async Task SendErrorEmail_LogsInformation()
        {
            // arrange
            var exception = new Exception("Test");
            _mockSmtpClient.Setup(s => s.SendMailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // act
            await _emailService.SendErrorEmail(exception, _cts.Token);

            // assert
            _mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Sending Error Email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        #endregion

        #region Configuration Tests

        [Test]
        public async Task SendEmail_WithMissingSmtpUsername_ReturnsFalseAndLogsWarning()
        {
            // arrange
            var badConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"Smtp:ToAddress", "recipient@example.com"}
                })
                .Build();

            var service = new EmailService(_mockLogger.Object, badConfig, _mockSmtpClient.Object);

            // act
            var result = await service.SendInfoEmail("Test", _cts.Token);

            // assert
            Assert.That(result, Is.False);
            _mockSmtpClient.Verify(s => s.SendMailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("email config is incomplete")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Test]
        public async Task SendEmail_WithMissingToAddress_ReturnsFalseAndLogsWarning()
        {
            // arrange
            var badConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"Smtp:Username", "test@example.com"}
                })
                .Build();

            var service = new EmailService(_mockLogger.Object, badConfig, _mockSmtpClient.Object);

            // act
            var result = await service.SendInfoEmail("Test", _cts.Token);

            // assert
            Assert.That(result, Is.False);
            _mockSmtpClient.Verify(s => s.SendMailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public async Task SendEmail_WhenSmtpClientThrows_ReturnsFalseAndLogsError()
        {
            // arrange
            _mockSmtpClient.Setup(s => s.SendMailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("SMTP error"));

            // act
            var result = await _emailService.SendInfoEmail("Test", _cts.Token);

            // assert
            Assert.That(result, Is.False);
            _mockLogger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error sending email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        #endregion
    }
}