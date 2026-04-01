namespace NeoConnect
{
    public interface IScheduledAction
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        string? Schedule { get; }
        bool IsRunning { get; }
        Task Run(CancellationToken stoppingToken);
    }
}
