using Cronos;

namespace NeoConnect
{
    public class Worker : BackgroundService
    {
        private const string schedule = "*/1 * * * *"; // every 5 minutes

        private readonly ILogger<Worker> _logger;        
        private readonly ActionsService _actionsService;              
        private readonly CronExpression _cron;

        public Worker(ILogger<Worker> logger, ActionsService actionsService)
        {
            _logger = logger;
            _actionsService = actionsService;

            _cron = CronExpression.Parse(schedule);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker started. Waiting for next scheduled run...");

            bool isFirstRun = true;
            while (!stoppingToken.IsCancellationRequested)
            {
                var utcNow = DateTime.UtcNow;
                var nextUtc = _cron.GetNextOccurrence(utcNow) ?? utcNow;

                if (isFirstRun)
                {
                    isFirstRun = false;
                }
                else
                {
                    await Task.Delay(nextUtc - utcNow, stoppingToken);
                }

                await _actionsService.SetPreheatDurationBasedOnWeatherConditions(stoppingToken);
            }
        }
    }
}
