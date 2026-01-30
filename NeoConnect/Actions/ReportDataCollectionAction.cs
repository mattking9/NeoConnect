namespace NeoConnect
{
    /// <summary>
    /// Represents a scheduled action that collects device status data for reporting purposes.
    /// </summary>
    /// <remarks>This action is designed to be executed on a schedule defined in the application
    /// configuration. It initializes the heating service, reports device statuses, and performs cleanup
    /// operations.</remarks>
    public class ReportDataCollectionAction : IScheduledAction
    {
        private readonly IConfiguration _config;        
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ReportDataCollectionAction(IConfiguration config, IServiceScopeFactory serviceScopeFactory)
        {
            _config = config;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public string? Name => "Report Data Collection";

        public string? Schedule => _config["ReportDataCollectionSchedule"];

        public async Task Action(CancellationToken stoppingToken)
        {            
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var heatingService = scope.ServiceProvider.GetService<IHeatingService>();
                
                await heatingService.LogDeviceStatuses(stoppingToken);
            }
        }
    }
}
