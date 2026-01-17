using NeoConnect;
using NUnit.Framework;

namespace NeoConnect.UnitTests
{
    [TestFixture]
    public class ReportDataServiceTests
    {
        private ReportDataService _service;

        [SetUp]
        public void Setup()
        {
            _service = new ReportDataService(null);
        }

        [Test]
        public void ToHtmlReportString_WhenNoData_ReturnsNoDataMessage()
        {
            // Act
            var result = _service.ToHtmlReportString();

            // Assert
            Assert.That(result, Does.Contain("<p>Report contains no data</p>"));
            Assert.That(result, Does.Contain("<h1>NeoConnect Report</h1>"));
        }

        [Test]
        public void ToHtmlReportString_WhenSingleStringAdded_ReturnsHtmlWithOneItem()
        {
            // Arrange
            _service.Add("Test entry");

            // Act
            var result = _service.ToHtmlReportString();

            // Assert
            Assert.That(result, Does.Contain("<li>Test entry</li>"));
            Assert.That(result, Does.Contain("<ol>"));
            Assert.That(result, Does.Contain("<h2>"));
        }

        [Test]
        public void ToHtmlReportString_WhenMultipleStringsAdded_ReturnsHtmlWithAllItems()
        {
            // Arrange
            var items = new[] { "First", "Second", "Third" };
            _service.Add(items);

            // Act
            var result = _service.ToHtmlReportString();

            // Assert
            foreach (var item in items)
            {
                Assert.That(result, Does.Contain($"<li>{item}</li>"));
            }
            Assert.That(result, Does.Contain("<ol>"));
        }

        [Test]
        public void ToHtmlReportString_AfterClear_ReturnsNoDataMessage()
        {
            // Arrange
            _service.Add("Should be cleared");
            _service.Clear();

            // Act
            var result = _service.ToHtmlReportString();

            // Assert
            Assert.That(result, Does.Contain("<p>Report contains no data</p>"));
        }
    }
}