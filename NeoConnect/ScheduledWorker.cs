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
                var offsetNow = DateTimeOffset.Now;
                var nextRun = cron.GetNextOccurrence(offsetNow, TimeZoneInfo.Local) ?? offsetNow;

                _logger.LogInformation($"{_action.Name}: Next run scheduled for " + nextRun.ToString("dd/MM/yyyy HH:mm:ss"));
                await Task.Delay(nextRun - offsetNow, stoppingToken);
                
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

                    await _emailService.SendErrorEmail(ex, stoppingToken);
                }
            }
        }
    }
}
