using System.Net;
using System.Net.Mail;

namespace NeoConnect
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _config;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _smtpToAddress;

        public EmailService(ILogger<EmailService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;

            _smtpHost = _config.GetValue<string>("Smtp:Host") ?? throw new ArgumentNullException("Config value for Smtp:Host is required");
            _smtpPort = _config.GetValue<int>("Smtp:Port");
            _smtpUsername = _config.GetValue<string>("Smtp:Username") ?? throw new ArgumentNullException("Config value for Smtp:Username is required");
            _smtpPassword = _config.GetValue<string>("Smtp:Password") ?? throw new ArgumentNullException("Config value for Smtp:Password is required");
            _smtpToAddress = _config.GetValue<string>("Smtp:ToAddress") ?? throw new ArgumentNullException("Config value for Smtp:ToAddress is required");
        }

        public async Task SendSummaryEmail(List<string> deviceStatements, CancellationToken stoppingToken)
        {
            if (deviceStatements == null || !deviceStatements.Any())
            {
                return;
            }

            _logger.LogInformation("Sending Summary Email.");

            await SendEmail(
                "Neo Connect Summary",
                $"Neo Connect made the following changes: <ul>{string.Join("", deviceStatements.Select(x => $"<li>{x}</li>"))}</ul>",
                true,
                stoppingToken);
        }

        public async Task SendErrorEmail(Exception error, CancellationToken stoppingToken)
        {
            if (error == null)
            {
                return;
            }

            _logger.LogInformation("Sending Error Email.");

            await SendEmail(
                "Neo Connect Error",
                $"Neo Connect encountered the following error while executing: <h3>{error.Message}</h3><p>{error.StackTrace ?? "(Stack trace unavailable)"}</p>",
                true,
                stoppingToken);
        }

        private async Task SendEmail(string subject, string body, bool isHtml, CancellationToken stoppingToken)
        {
            try
            {
                using (var smtpClient = new SmtpClient(_smtpHost, _smtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);

                    using (var mailMessage = new MailMessage(_smtpUsername, _smtpToAddress)
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = isHtml
                    })
                    {
                        await smtpClient.SendMailAsync(mailMessage, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
            }
        }
    }
}
