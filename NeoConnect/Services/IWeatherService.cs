
namespace NeoConnect
{
    public interface IWeatherService
    {
        Task<Forecast> GetForecast(CancellationToken stoppingToken);
    }
}