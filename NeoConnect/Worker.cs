using Cronos;

namespace NeoConnect
{
    public class Worker : BackgroundService
    {
        private readonly ActionsService _actionsService;
        private readonly IConfiguration _config;

        public Worker(IConfiguration config, ActionsService actionsService)
        {
            _config = config;
            _actionsService = actionsService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine(@" _   _             ____                            _   ");
            Console.WriteLine(@"| \ | | ___  ___  / ___|___  _ __  _ __   ___  ___| |_ ");
            Console.WriteLine(@"|  \| |/ _ \/ _ \| |   / _ \| '_ \| '_ \ / _ \/ __| __|");
            Console.WriteLine(@"| |\  |  __/ (_) | |__| (_) | | | | | | |  __/ (__| |_ ");
            Console.WriteLine(@"|_| \_|\___|\___/ \____\___/|_| |_|_| |_|\___|\___|\__|");
            Console.WriteLine("");

            var schedule = _config["Schedule"];
            CronExpression? _cron = null;                        
            if (schedule == null || !CronExpression.TryParse(schedule, CronFormat.Standard, out _cron))
            {
                // No schedule is configured, run immediately.
                await _actionsService.PerformActions(stoppingToken);
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var utcNow = DateTime.UtcNow;
                var nextRunUtc = _cron.GetNextOccurrence(utcNow) ?? utcNow;
                
                Console.WriteLine("Next run scheduled for " + nextRunUtc.ToString("G"));

                await Task.Delay(nextRunUtc - utcNow, stoppingToken);                
                
                Console.WriteLine("");
            }
        }
    }
}
