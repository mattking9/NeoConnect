
namespace NeoConnect
{
    public interface IEmailService
    {
        Task<bool> TrySendErrorEmail(Exception error, CancellationToken stoppingToken);
        Task SendSummaryEmail(List<string> deviceStatements, CancellationToken stoppingToken);
    }
}