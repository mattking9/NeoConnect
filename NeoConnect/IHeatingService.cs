
namespace NeoConnect
{
    public interface IHeatingService
    {
        Task Cleanup(CancellationToken stoppingToken);
        Task Init(CancellationToken stoppingToken);
        Task ReduceSetTempWhenExternalTempIsWarm(ForecastDay forecastToday, CancellationToken stoppingToken);
        Task BoostTowelRailWhenBathroomIsCold(CancellationToken stoppingToken);
    }
}