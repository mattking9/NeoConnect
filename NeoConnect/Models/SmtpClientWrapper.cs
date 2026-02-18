using System.Net;
using System.Net.Mail;

namespace NeoConnect
{
    public class SmtpClientWrapper : ISmtpClientWrapper
    {
        private readonly IConfiguration _config;

        public SmtpClientWrapper(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendMailAsync(string from, string to, string subject, string body, bool isBodyHtml, CancellationToken cancellationToken)
        {
            var smtpHost = _config["Smtp:Host"];
            var smtpPort = _config["Smtp:Port"];
            var smtpUsername = _config["Smtp:Username"];
            var smtpPassword = _config["Smtp:Password"];

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPort))
            {
                throw new InvalidOperationException("SMTP host and port must be configured.");
            }

            using var smtpClient = new SmtpClient(smtpHost, int.Parse(smtpPort))
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };

            using var mailMessage = new MailMessage(from, to)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = isBodyHtml
            };

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
        }
    }
}