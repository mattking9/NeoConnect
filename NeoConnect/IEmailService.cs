
namespace NeoConnect
{
    public interface IEmailService
    {
        Task SendErrorEmail(Exception error, CancellationToken stoppingToken);
        Task SendSummaryEmail(List<string> deviceStatements, CancellationToken stoppingToken);
    }
}