using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace NeoConnect.UnitTests
{
    [TestFixture]
    public class SyntheticTests
    {
        private HeatingService _heatingService;
        private Mock<INeoHubService> _mockNeoHubService;
        private Mock<ILogger<HeatingService>> _mockLogger;
        private Mock<IOptions<HeatingConfig>> _mockOptions;
        private HeatingConfig _config;
        private CancellationTokenSource _cts;
        private NeoDevice _device;


        [SetUp]
        public void Setup()
        {
            _mockNeoHubService = new Mock<INeoHubService>();
            _mockLogger = new Mock<ILogger<HeatingService>>();

            _config = new HeatingConfig
            {
                SummerProfileName = "Summer",
                PreHeatOverride = new PreHeatOverrideConfig
                {
                    MaxPreheatHours = 4,                    
                    Overrides = new List<MaxPreHeatOverride>()
                    {
                        new MaxPreHeatOverride { ExternalTempAbove = 9.5m, MaxPreheatHours = 1 },
                        new MaxPreHeatOverride { ExternalTempAbove = 6.5m, MaxPreheatHours = 2 },
                    }
                },
                Recipes = new RecipeConfig
                {
                    ExternalTempThreshold = 15.0m,
                    SummerRecipeName = "Summer Recipe",
                    WinterRecipeName = "Winter Recipe"
                }
            };
            _mockOptions = new Mock<IOptions<HeatingConfig>>();
            _mockOptions.Setup(o => o.Value).Returns(_config);

            _device = new NeoDevice
            {
                ZoneName = "Lounge",
                IsThermostat = true,
                IsOffline = false,
                ActiveProfile = 1,
            };
            _mockNeoHubService.Setup(s => s.GetDevices(It.IsAny<CancellationToken>())).ReturnsAsync(new List<NeoDevice>() { _device });

            var nextInterval = new ComfortLevel(new object[] { "07:00:00", "20" });
            _mockNeoHubService.Setup(s => s.GetNextSwitchingInterval(It.IsAny<ProfileSchedule>(), It.IsAny<DateTime?>())).Returns(nextInterval);

            var profiles = new Dictionary<int, Profile>
            {
                {
                    1, new Profile { ProfileId = 1, ProfileName = "Winter" }
                }
            };
            _mockNeoHubService.Setup(s => s.GetAllProfiles(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);

            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Lounge", new EngineersData { MaxPreheatDuration = 0 } } // Preheat duration set to 0 hours
            };            
            _mockNeoHubService.Setup(s => s.GetEngineersData(It.IsAny<CancellationToken>())).ReturnsAsync(engineersData);            
            
            _cts = new CancellationTokenSource();                        
        }

        [TearDown]
        public void TearDown()
        {
            _cts.Dispose();
        }
        
        [Test]
        public async Task SyntheticTest()
        {
            // Arrange
            double tests = 0;
            double passes = 0;

            //This table maps the effect of external temperature on the rate of change (ROC) in internal temperature when heating.
            var externalTemperatureWeightings = new Dictionary<decimal, decimal>()
            {
                { 13, 0.9m },
                { 12, 0.7m },
                { 11, 0.5m },
                { 10, 0.3m },
                { 9, 0.2m },
                { 8, 0.1m },
                { 7, 0.08m },
                { 6, 0.05m },
                { 5, 0m },
                { 4, 0m },                
            };

            foreach (var externalTempWeighting in externalTemperatureWeightings)
            {                
                var externalTemp = externalTempWeighting.Key;
                var weighting = externalTempWeighting.Value;

                var forecastToday = CreateForecastHours(0, 0, 0, 0, 0, 0, 0, externalTemp, externalTemp);

                var weightedRoc = 85 * (1 - weighting);

                foreach (var actualTemp in new[] { 19.5m, 18.5m, 17.5m, 16.5m })
                {
                    tests++;

                    _device.ActualTemp = actualTemp.ToString();

                    var timeToTargetTemp = (20.0m - actualTemp) * weightedRoc; 
                    var expectedPreheatDuration = Math.Round(timeToTargetTemp / 60, 2);

                    // Act
                    _heatingService = new HeatingService(_mockOptions.Object, _mockLogger.Object, _mockNeoHubService.Object);
                    await _heatingService.SetPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

                    // Assert
                    try
                    {
                        _mockNeoHubService.Verify(s => s.SetPreheatDuration(
                            "Lounge", 
                            It.Is<int>(p => p >= expectedPreheatDuration - 0.3m), 
                            It.IsAny<CancellationToken>()), Times.Once);

                        Console.WriteLine($"PASS: External Temp: {externalTemp}c, Internal Temp Dif: {20 - actualTemp}c");
                        passes++;
                    }
                    catch (Exception ex)
                    {
                        var changes = _heatingService.GetChangesMade();
                        Console.WriteLine($"FAIL: External Temp: {externalTemp}c, Internal Temp Dif: {20 - actualTemp}c, Required: {expectedPreheatDuration}, Actual: {changes.Single()}");
                    }

                    _mockNeoHubService.Invocations.Clear();
                }
            }

            double rate = passes / tests;

            Console.WriteLine($"Pass rate: {Math.Round(rate * 100, 2)}% ({passes} out of {tests})");

            Assert.That(rate >= 0.85, "Success rate was " + Math.Round(rate, 2));
        }                
        
        private ForecastDay CreateForecastHours(params decimal[] hourlyTemps)
        {
            return new ForecastDay
            {
                Hour = hourlyTemps.Select(x => new ForecastHour { Temp = x }).ToList()
            };                     
        }
    }
}