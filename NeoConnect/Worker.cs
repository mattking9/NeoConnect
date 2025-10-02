using Cronos;

namespace NeoConnect
{
    public class Worker : BackgroundService
    {        
        private readonly IConfiguration _config;
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public Worker(ILogger<Worker> logger, IConfiguration config, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _config = config;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var schedule = _config["Schedule"];
            CronExpression? cron = null;
            
            if (schedule == null || !CronExpression.TryParse(schedule, CronFormat.Standard, out cron))
            {
                _logger.LogError($"Unable to parse schedule '{schedule}'. Exiting.");
                Environment.Exit(-1);
            }
        
            while (!stoppingToken.IsCancellationRequested)
            {
                var utcNow = DateTime.UtcNow;
                var nextRunUtc = cron.GetNextOccurrence(utcNow) ?? utcNow;

                _logger.LogInformation("Next run scheduled for " + nextRunUtc.ToLocalTime().ToString("G"));
                await Task.Delay(nextRunUtc - utcNow, stoppingToken);

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var actionsService = scope.ServiceProvider.GetRequiredService<Actions>();
                    await actionsService.PerformActions(stoppingToken);
                }
            }
        }
    }
}
