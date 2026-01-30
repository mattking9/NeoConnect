

namespace NeoConnect
{
    public interface IHeatingService
    {
        Task ReduceSetTempWhenExternalTempIsWarm(ForecastDay forecastToday, CancellationToken stoppingToken);
        Task BoostTowelRailWhenBathroomIsCold(CancellationToken stoppingToken);
        Task LogDeviceStatuses(CancellationToken stoppingToken);
        Task<List<NeoDevice>> GetDevices(CancellationToken stoppingToken);
        Task<Dictionary<int, Profile>> GetProfiles(CancellationToken stoppingToken);
    }
}