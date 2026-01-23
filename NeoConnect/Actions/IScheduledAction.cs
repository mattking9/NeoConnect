namespace NeoConnect
{
    public interface IScheduledAction
    {
        string? Name { get; }
        string? Schedule { get; }
        Task Action(CancellationToken stoppingToken);
    }
}
