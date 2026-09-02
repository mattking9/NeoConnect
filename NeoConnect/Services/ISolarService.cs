namespace NeoConnect
{
    public interface ISolarService
    {
        Task<SolarData> GetRealtimeData(CancellationToken stoppingToken);
        Task SetForceChargeWindow(int enable, int startHour, int startMinute, int endHour, int endMinute, CancellationToken stoppingToken);
    }
}