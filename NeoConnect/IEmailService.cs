
namespace NeoConnect
{
    public interface IEmailService
    {
        Task<bool> TrySendErrorEmail(Exception error, CancellationToken stoppingToken);
        Task SendEmail(string subject, string body, CancellationToken stoppingToken);
    }
}