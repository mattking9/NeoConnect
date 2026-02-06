

namespace NeoConnect
{
    public interface IHeatingService
    {
        Task ReduceSetTempWhenExternalTempIsWarm(ForecastDay forecastToday, CancellationToken stoppingToken);
        Task BoostTowelRailWhenBathroomIsCold(CancellationToken stoppingToken);
        Task LogDeviceStatuses(CancellationToken stoppingToken);
        Task<IEnumerable<Device>> GetDevices(CancellationToken stoppingToken);
        Task<IEnumerable<DeviceHistory>> GetDeviceHistory(DateTime date, CancellationToken stoppingToken);
        Task<IEnumerable<Schedule>> GetSchedules(CancellationToken stoppingToken);
    }
}