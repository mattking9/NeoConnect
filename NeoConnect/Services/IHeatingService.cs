

namespace NeoConnect
{
    public interface IHeatingService
    {
        Task GlobalHold(double adjustment, int hours, CancellationToken stoppingToken);
        Task BoostTowelRailWhenBathroomIsCold(CancellationToken stoppingToken);
        Task LogDeviceStatuses(CancellationToken stoppingToken);
        Task<IEnumerable<Device>> GetDevices(bool includeAdvancedData, CancellationToken stoppingToken);
        Task<IEnumerable<DeviceHistory>> GetDeviceHistory(DateTime date);
        Task<IEnumerable<Schedule>> GetSchedules(CancellationToken stoppingToken);        
        Task SetTemperature(string deviceName, double temp, CancellationToken stoppingToken);
        Task RefreshDeviceList(CancellationToken stoppingToken);
        Task TurnOffHotWater(int hours, CancellationToken stoppingToken);
    }
}