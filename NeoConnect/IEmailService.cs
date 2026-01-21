
namespace NeoConnect
{
    public interface IEmailService
    {
        Task<bool> SendErrorEmail(Exception error, CancellationToken stoppingToken);
        Task<bool> SendInfoEmail(IEnumerable<string> items, CancellationToken stoppingToken);
        Task<bool> SendInfoEmail(string info, CancellationToken stoppingToken);
    }
}