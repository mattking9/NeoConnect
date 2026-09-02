namespace NeoConnect
{
    public interface ISolarForecastService
    {
        Task<double> GetSolarEstimate(CancellationToken stoppingToken);
    }
}