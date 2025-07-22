using Cronos;

namespace NeoConnect
{
    public class Worker : BackgroundService
    {
        private const string schedule = "*/1 * * * *"; // every 1 minute

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
            Console.WriteLine(@" _   _             ____                            _   ");
            Console.WriteLine(@"| \ | | ___  ___  / ___|___  _ __  _ __   ___  ___| |_ ");
            Console.WriteLine(@"|  \| |/ _ \/ _ \| |   / _ \| '_ \| '_ \ / _ \/ __| __|");
            Console.WriteLine(@"| |\  |  __/ (_) | |__| (_) | | | | | | |  __/ (__| |_ ");
            Console.WriteLine(@"|_| \_|\___|\___/ \____\___/|_| |_|_| |_|\___|\___|\__|");
            
            Console.WriteLine("");
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
                
                await _actionsService.PerformActions(stoppingToken);

                Console.WriteLine("");
            }
        }
    }
}
