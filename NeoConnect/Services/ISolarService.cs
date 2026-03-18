namespace NeoConnect
{
    public interface ISolarService
    {
        Task<SolarData> GetRealtimeData();
    }
}