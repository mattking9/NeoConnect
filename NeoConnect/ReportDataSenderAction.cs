namespace NeoConnect
{
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
            await _emailService.SendEmail("NeoConnect Report", _reportDataService.ToHtmlReportString(), stoppingToken);
        }
    }
}
