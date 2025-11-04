
namespace NeoConnect
{
    public interface IEmailService
    {
        Task<bool> SendErrorEmail(Exception error, CancellationToken stoppingToken);
        Task<bool> SendEmail(string subject, string body, CancellationToken stoppingToken);
    }
}