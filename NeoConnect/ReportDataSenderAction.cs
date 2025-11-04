namespace NeoConnect
{
    /// <summary>
    /// Represents a scheduled action that sends the gathered reporting data by email.
    /// </summary>
    /// <remarks>This action is designed to be executed on a schedule defined in the application
    /// configuration. It initializes the heating service, reports device statuses, and performs cleanup
    /// operations.</remarks>
    public class ReportDataSenderAction : IScheduledAction
    {
        private readonly IConfiguration _config;        
        private readonly IEmailService _emailService;
        private readonly IReportDataService _reportDataService;

        public ReportDataSenderAction(IConfiguration config, IEmailService emailService, IReportDataService reportDataService)
        {
            _config = config;
            _emailService = emailService;
            _reportDataService = reportDataService;
        }

        public string? Name => "Report Data Sender";

        public string? Schedule => _config["ReportDataSenderSchedule"];

        public async Task Action(CancellationToken stoppingToken)
        {
            if (await _emailService.SendEmail("NeoConnect Report", _reportDataService.ToHtmlReportString(), stoppingToken))
                _reportDataService.Clear();
        }
    }
}
