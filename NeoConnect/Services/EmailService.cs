using System.Text;

namespace NeoConnect
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _config;
        private readonly ISmtpClientWrapper _smtpClient;

        public EmailService(ILogger<EmailService> logger, IConfiguration config, ISmtpClientWrapper smtpClient)
        {
            _logger = logger;
            _config = config;
            _smtpClient = smtpClient;
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
                $"Neo Connect encountered the following error: <h3>{error?.Message}</h3><p>Please check any devices that might be left in an incomplete state.</p><p>{error?.StackTrace ?? "(Stack trace unavailable)"}</p>",
                true,
                stoppingToken);            
        }

        private async Task<bool> SendEmail(string subject, string body, bool isHtml, CancellationToken stoppingToken)
        {
            try
            {
                var smtpUsername = _config["Smtp:Username"];
                var smtpToAddress = _config["Smtp:ToAddress"];

                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpToAddress))
                {
                    _logger.LogWarning($"Unable to send email '{subject}' as email config is incomplete.");
                    return false;
                }

                await _smtpClient.SendMailAsync(smtpUsername, smtpToAddress, subject, body, isHtml, stoppingToken);

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
