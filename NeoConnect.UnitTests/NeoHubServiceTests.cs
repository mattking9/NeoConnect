using Moq;

namespace NeoConnect.UnitTests
{
    [TestFixture]
    public class NeoHubServiceTests
    {
        ProfileSchedule schedule;
        [SetUp]
        public void Setup()
        {
            schedule = new ProfileSchedule
            {
                Weekdays = new ProfileScheduleGroup
                {
                    Wake = new object[] { "07:00", "20.5" },
                    Leave = new object[] { "08:00", "20.4" },                                        
                    Return = new object[] { "17:00", "20.3" },
                    Sleep = new object[] { "22:00", "20.2" },
                },
                Weekends = new ProfileScheduleGroup
                {
                    Wake = new object[] { "07:30", "20.9" },
                    Leave = new object[] { "08:30", "20.8" },
                    Return = new object[] { "17:30", "20.7" },
                    Sleep = new object[] { "22:30", "20.6" },
                },
            };
        }

        [TestCase("2025-07-21 00:00:00", "07:00", 20.5)] // Monday
        [TestCase("2025-07-21 06:59:59", "07:00", 20.5)]
        [TestCase("2025-07-21 07:00:00", "08:00", 20.4)]
        [TestCase("2025-07-21 07:59:59", "08:00", 20.4)]
        [TestCase("2025-07-21 08:00:00", "17:00", 20.3)]
        [TestCase("2025-07-21 16:59:59", "17:00", 20.3)]
        [TestCase("2025-07-21 17:00:00", "22:00", 20.2)]
        [TestCase("2025-07-21 21:59:59", "22:00", 20.2)]
        [TestCase("2025-07-25 00:00:00", "07:00", 20.5)] // Friday
        [TestCase("2025-07-25 06:59:59", "07:00", 20.5)]
        [TestCase("2025-07-25 07:00:00", "08:00", 20.4)]
        [TestCase("2025-07-25 07:59:59", "08:00", 20.4)]
        [TestCase("2025-07-25 08:00:00", "17:00", 20.3)]
        [TestCase("2025-07-25 16:59:59", "17:00", 20.3)]
        [TestCase("2025-07-25 17:00:00", "22:00", 20.2)]
        [TestCase("2025-07-25 21:59:59", "22:00", 20.2)]
        [TestCase("2025-07-26 00:00:00", "07:30", 20.9)] // Saturday
        [TestCase("2025-07-26 07:29:59", "07:30", 20.9)]
        [TestCase("2025-07-26 07:30:00", "08:30", 20.8)]
        [TestCase("2025-07-26 08:29:59", "08:30", 20.8)]
        [TestCase("2025-07-26 08:30:00", "17:30", 20.7)]
        [TestCase("2025-07-26 17:29:59", "17:30", 20.7)]
        [TestCase("2025-07-26 17:30:00", "22:30", 20.6)]
        [TestCase("2025-07-26 22:29:59", "22:30", 20.6)]
        [TestCase("2025-07-27 00:00:00", "07:30", 20.9)] // Sunday
        [TestCase("2025-07-27 07:29:59", "07:30", 20.9)]
        [TestCase("2025-07-27 07:30:00", "08:30", 20.8)]
        [TestCase("2025-07-27 08:29:59", "08:30", 20.8)]
        [TestCase("2025-07-27 08:30:00", "17:30", 20.7)]
        [TestCase("2025-07-27 17:29:59", "17:30", 20.7)]
        [TestCase("2025-07-27 17:30:00", "22:30", 20.6)]
        [TestCase("2025-07-27 22:29:59", "22:30", 20.6)]

        public void GetNextSwitchingInterval_ReturnsCorrectResult(string isoDateTime, string expectedSwitchingTime, double expectedTemp)
        {
            // Arrange
            var service = new NeoHubService();

            // Act
            var result = service.GetNextSwitchingInterval(schedule, DateTime.Parse(isoDateTime));

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Time == TimeOnly.Parse(expectedSwitchingTime));
            Assert.That(result.TargetTemp == expectedTemp);
        }

        [TestCase("2025-07-21 22:00:00")] //Monday
        [TestCase("2025-07-25 22:00:00")] //Friday
        [TestCase("2025-07-26 22:30:00")] //Saturday
        [TestCase("2025-07-27 22:30:00")] //Sunday
        public void GetNextSwitchingInterval_NoRemainingIntervals_ReturnsNull(string isoDateTime)
        {
            // Arrange
            var service = new NeoHubService();

            // Act
            var result = service.GetNextSwitchingInterval(schedule, DateTime.Parse(isoDateTime));

            // Assert
            Assert.That(result, Is.Null);
        }
    }
}