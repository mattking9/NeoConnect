namespace NeoConnect
{
    /// <summary>
    /// Represents a scheduled action that collects device status data for reporting purposes.
    /// </summary>
    /// <remarks>This action is designed to be executed on a schedule defined in the application
    /// configuration. It initializes the heating service, reports device statuses, and performs cleanup
    /// operations.</remarks>
    public class ReportDataCollectionAction : ScheduledAction
    {
        private readonly IConfiguration _config;        
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ReportDataCollectionAction(IConfiguration config, IServiceScopeFactory serviceScopeFactory, ILogger<ReportDataCollectionAction> logger, IEmailService emailService) 
            : base (logger, emailService)
        {
            _config = config;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public override string Id => "data_collection";

        public override string Name => "Report Data Collection";

        public override string Description => "Periodically gathers status data on all devices and saves them to the database for reporting purposes.";

        public override string? Schedule => _config["ReportDataCollectionSchedule"];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {            
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var heatingService = scope.ServiceProvider.GetRequiredService<IHeatingService>();
                
                await heatingService.LogDeviceStatuses(stoppingToken);
            }
        }
    }
}
