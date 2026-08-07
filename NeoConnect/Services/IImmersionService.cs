namespace NeoConnect
{
    public interface IImmersionService
    {
        Device GetDevice();
        Task TurnOffDevice(CancellationToken stoppingToken);
        Task TurnOnDevice(CancellationToken stoppingToken);
    }
}