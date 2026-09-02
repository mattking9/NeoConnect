namespace NeoConnect
{
    public interface ISolarService
    {
        Task<SolarData> GetRealtimeData(CancellationToken stoppingToken);
        Task SetForceChargeWindow(int startHour, int startMinute, int endHour, int endMinute, int targetSoc, CancellationToken stoppingToken);
    }
}