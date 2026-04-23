
namespace NeoConnect
{
    public interface IWeatherService
    {
        Task<WeatherResponse> GetForecast(CancellationToken stoppingToken);
    }
}