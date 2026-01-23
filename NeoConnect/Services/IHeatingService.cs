

namespace NeoConnect
{
    public interface IHeatingService
    {
        Task Cleanup(CancellationToken stoppingToken);
        Task Init(CancellationToken stoppingToken);
        Task ReduceSetTempWhenExternalTempIsWarm(ForecastDay forecastToday, CancellationToken stoppingToken);
        Task BoostTowelRailWhenBathroomIsCold(CancellationToken stoppingToken);
        Task LogDeviceStatuses(CancellationToken stoppingToken);
        Task<List<NeoDevice>> GetDevices(CancellationToken stoppingToken);
    }
}