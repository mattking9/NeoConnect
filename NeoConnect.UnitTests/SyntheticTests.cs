using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace NeoConnect.UnitTests
{
    [TestFixture]
    public class SyntheticTests
    {        
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
                    ExternalTempROCWeightings = new List<TemperatureWeighting>()
                    {
                        new TemperatureWeighting { Temp = 4, Weighting = 0.7 },
                        new TemperatureWeighting { Temp = 6, Weighting = 0.5 },
                        new TemperatureWeighting { Temp = 8, Weighting = 0.3 },
                        new TemperatureWeighting { Temp = 10, Weighting = 0.1 },
                        new TemperatureWeighting { Temp = 12, Weighting = 0 },
                    }                    
                },
                Recipes = new RecipeConfig
                {
                    ExternalTempThreshold = 15.0,
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

            var rocs = new Dictionary<string, int>
            {
                {
                    "Lounge", 85
                }
            };
            _mockNeoHubService.Setup(s => s.GetROCData(It.IsAny<string[]>(), It.IsAny<CancellationToken>())).ReturnsAsync(rocs);

            var engineersData = new Dictionary<string, EngineersData>
            {
                { "Lounge", new EngineersData { MaxPreheatDuration = -1 } }
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
            var externalTemperatureWeightings = new Dictionary<double, double>()
            {
                { 14, 0 },
                { 13, 0.0 },
                { 12, 0.0 },
                { 11, 0.1 },
                { 10, 0.1 },
                { 9, 0.3 },
                { 8, 0.3 },
                { 7, 0.5 },
                { 6, 0.5 },
                { 5, 0.7 },
                { 4, 0.7 },
                { 3, 1 },
            };

            foreach (var externalTempWeighting in externalTemperatureWeightings)
            {                
                var externalTemp = externalTempWeighting.Key;
                var weighting = externalTempWeighting.Value;

                var forecastToday = CreateForecastHours(0, 0, 0, 0, 0, 0, 0, externalTemp - 0.1, externalTemp + 0.1);

                var weightedRoc = 85 * weighting;

                foreach (var actualTemp in new[] { 19.5, 18.5, 17.5, 16.5 })
                {
                    tests++;

                    _device.ActualTemp = actualTemp.ToString();

                    var timeToTargetTemp = (20.0 - actualTemp) * weightedRoc; 
                    var expectedPreheatDuration = Math.Round(timeToTargetTemp / 60, 2);

                    // Act                    
                    var heatingService = new HeatingService(_mockOptions.Object, _mockLogger.Object, _mockNeoHubService.Object);
                    await heatingService.SetMaxPreheatDurationBasedOnWeatherConditions(forecastToday, _cts.Token);

                    // Assert
                    var success = false;
                    try
                    {
                        _mockNeoHubService.Verify(s => s.SetPreheatDuration(
                            "Lounge", 
                            It.Is<int>(p => p >= expectedPreheatDuration - 0.3 && p <= expectedPreheatDuration + 1), 
                            It.IsAny<CancellationToken>()), Times.Once);
                        
                        success = true;
                        passes++;
                    }
                    catch (Exception ex)
                    {                        
                    }

                    var changes = heatingService.GetChangesMade();
                    Console.WriteLine($"{(success ? "PASS" : "FAIL")}: External Temp: {externalTemp}c, Internal Temp Dif: {20 - actualTemp}c, Required: {expectedPreheatDuration}, Actual: {changes.Single()}");

                    _mockNeoHubService.Invocations.Clear();
                }
            }

            double rate = passes / tests;

            Console.WriteLine($"Pass rate: {Math.Round(rate * 100, 2)}% ({passes} out of {tests})");

            Assert.That(rate >= 0.85, "Success rate was " + Math.Round(rate, 2));
        }                
        
        private ForecastDay CreateForecastHours(params double[] hourlyTemps)
        {
            return new ForecastDay
            {
                Hour = hourlyTemps.Select(x => new ForecastHour { Temp = x, IsDaytime = 1, Condition = new ForecastCondition() { Code = 1000 } }).ToList()
            };                     
        }
    }
}