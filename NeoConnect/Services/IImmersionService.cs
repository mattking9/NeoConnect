namespace NeoConnect
{
    public interface IImmersionService
    {
        Task TurnOffDevice(CancellationToken stoppingToken);
        Task TurnOnDevice(CancellationToken stoppingToken);
    }
}