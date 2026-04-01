using Cronos;

namespace NeoConnect
{

    public class ScheduledWorker<TAction> : BackgroundService where TAction : IScheduledAction
    {        
        private readonly IScheduledAction _action;
        private readonly ILogger<ScheduledWorker<TAction>> _logger;
        

        public ScheduledWorker(TAction action, ILogger<ScheduledWorker<TAction>> logger)
        {
            _action = action;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {    
            var schedule = _action.Schedule;
            CronExpression? cron = null;
            
            if (schedule == null || !CronExpression.TryParse(schedule, CronFormat.Standard, out cron))
            {
                _logger.LogError($"Unable to parse schedule '{schedule}'. Exiting.");
                Environment.Exit(-1);
            }
        
            while (!stoppingToken.IsCancellationRequested)
            {
                var offsetNow = DateTimeOffset.Now;
                var nextRun = cron.GetNextOccurrence(offsetNow, TimeZoneInfo.Local) ?? offsetNow;

                // Delay until next scheduled run time
                _logger.LogInformation($"{_action.Name}: Next run scheduled for " + nextRun.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"));
                await Task.Delay(nextRun - offsetNow, stoppingToken);
                
                // Run Action
                await _action.Run(stoppingToken);
            }
        }
    }
}
