
namespace NeoConnect.Services
{
    public interface IWeatherService
    {
        Task<Forecast> GetForecast(CancellationToken stoppingToken);
    }
}