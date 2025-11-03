using Cronos;

namespace NeoConnect
{

    public class ScheduledWorker<TAction> : BackgroundService where TAction : IScheduledAction
    {        
        private readonly IScheduledAction _action;
        private readonly ILogger<ScheduledWorker<TAction>> _logger;
        private readonly IEmailService _emailService;
        

        public ScheduledWorker(TAction action, ILogger<ScheduledWorker<TAction>> logger, IEmailService emailService)
        {
            _action = action;
            _logger = logger;
            _emailService = emailService;
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
                var utcNow = DateTime.UtcNow;
                var nextRunUtc = cron.GetNextOccurrence(utcNow) ?? utcNow;

                _logger.LogInformation("Next run scheduled for " + nextRunUtc.ToLocalTime().ToString("G"));
                await Task.Delay(nextRunUtc - utcNow, stoppingToken);
                
                try
                {
                    _logger.LogInformation($"** {_action.Name} **");

                    await _action.Action(stoppingToken);

                    _logger.LogInformation("Process Completed.");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Operation was canceled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Execution Error. Aborting.");

                    await _emailService.TrySendErrorEmail(ex, stoppingToken);
                }
            }
        }
    }
}
