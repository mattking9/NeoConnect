using System.Net;
using System.Net.Mail;

namespace NeoConnect
{
    public class EmailService
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
            if(deviceStatements == null || !deviceStatements.Any())
            {            
                return;
            }

            _logger.LogInformation("Sending Summary Email...");

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
                        Subject = "Neo Connect Summary",
                        Body = $"<ul>{string.Join("", deviceStatements.Select(x => $"<li>{x}</li>"))}</ul>",
                        IsBodyHtml = true // Or false, depending on your email content
                    })
                    {
                        smtpClient.Send(mailMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception (log it, show an error message, etc.)
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }
    }
}
