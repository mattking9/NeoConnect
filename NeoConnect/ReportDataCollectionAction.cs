namespace NeoConnect
{
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

                await heatingService.Init(stoppingToken);

                await heatingService.ReportDeviceStatuses(stoppingToken);

                await heatingService.Cleanup(stoppingToken);
            }
        }
    }
}
