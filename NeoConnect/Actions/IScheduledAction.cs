namespace NeoConnect
{
    public interface IScheduledAction
    {
        string? Id { get; }
        string? Name { get; }
        string? Schedule { get; }
        Task Action(CancellationToken stoppingToken);
    }
}
