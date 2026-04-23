using Microsoft.AspNetCore.Mvc;

namespace NeoConnect
{
    /// <summary>
    /// Controller for retrieving weather information.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;        

        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherController"/> class.
        /// </summary>
        /// <param name="weatherService">The weather service.</param>
        /// <param name="logger">The logger.</param>
        public WeatherController(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        /// <summary>
        /// Gets the current weather forecast.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The current weather conditions.</returns>
        [HttpGet]
        public async Task<Current> GetCurrentWeather(CancellationToken cancellationToken)
        {
            var forecast = await _weatherService.GetForecast(cancellationToken);
            return forecast.Current;
        }
    }
}
