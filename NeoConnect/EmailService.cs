using System.Net;
using System.Net.Mail;
using System.Text;

namespace NeoConnect
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _config;

        public EmailService(ILogger<EmailService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task<bool> SendInfoEmail(string info, CancellationToken stoppingToken)
        {
            return await SendInfoEmail(new List<string>() { info }, stoppingToken);
        }

        public async Task<bool> SendInfoEmail(IEnumerable<string> items, CancellationToken stoppingToken)
        {
            if (items == null || items.Count() == 0)
            {
                _logger.LogInformation("No email body. Not sending Email.");
                return false;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("<html>");
            sb.Append("<p>NeoConnect made the following changes:</p>");

            sb.Append("<ul>");
            foreach (var val in items)
            {
                sb.Append($"<li>{val}</li>");
            }
            sb.Append("</ul>");
            sb.Append("</html>");

            _logger.LogInformation("Sending Email.");

            return await SendEmail("Neo Connect Made Changes", sb.ToString(), true, stoppingToken);
        }

        public async Task<bool> SendErrorEmail(Exception error, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Sending Error Email.");

            return await SendEmail(
                "Neo Connect Error",
                $"Neo Connect encountered the following error: <h3>{error?.Message}</h3><p>{error?.StackTrace ?? "(Stack trace unavailable)"}</p>",
                true,
                stoppingToken);            
        }

        private async Task<bool> SendEmail(string subject, string body, bool isHtml, CancellationToken stoppingToken)
        {
            try
            {
                var smtpHost = _config["Smtp:Host"];
                var smtpPort = _config["Smtp:Port"];
                var smtpUsername = _config["Smtp:Username"];
                var smtpPassword = _config["Smtp:Password"];
                var smtpToAddress = _config["Smtp:ToAddress"];

                if (string.IsNullOrEmpty(smtpHost)
                    || string.IsNullOrEmpty(smtpPort)
                    || string.IsNullOrEmpty(smtpUsername)
                    || string.IsNullOrEmpty(smtpPassword)
                    || string.IsNullOrEmpty(smtpToAddress))
                {
                    _logger.LogWarning($"Unable to send email '{subject}' as email config is incomplete.");
                    return false;
                }

                using (var smtpClient = new SmtpClient(smtpHost, int.Parse(smtpPort)))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                    using (var mailMessage = new MailMessage(smtpUsername, smtpToAddress)
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = isHtml
                    })
                    {
                        await smtpClient.SendMailAsync(mailMessage, stoppingToken);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                return false;
            }
        }
    }
}
