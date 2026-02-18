namespace NeoConnect
{
    public interface ISmtpClientWrapper
    {
        Task SendMailAsync(string from, string to, string subject, string body, bool isBodyHtml, CancellationToken cancellationToken);
    }
}