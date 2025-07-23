using Cronos;

namespace NeoConnect
{
    public class Worker : BackgroundService
    {
        private const string schedule = "0 2 * * *"; // every day at 2am

        private readonly ILogger<Worker> _logger;     
        private readonly ActionsService _actionsService;              
        private readonly CronExpression _cron;

        public Worker(ILogger<Worker> logger, IConfiguration config, ActionsService actionsService)
        {
            _logger = logger;
            _actionsService = actionsService;

            _cron = CronExpression.Parse(config["ExecutionSchedule"] ?? "0 2 * * *"); // default schedule: every day at 2am
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine(@" _   _             ____                            _   ");
            Console.WriteLine(@"| \ | | ___  ___  / ___|___  _ __  _ __   ___  ___| |_ ");
            Console.WriteLine(@"|  \| |/ _ \/ _ \| |   / _ \| '_ \| '_ \ / _ \/ __| __|");
            Console.WriteLine(@"| |\  |  __/ (_) | |__| (_) | | | | | | |  __/ (__| |_ ");
            Console.WriteLine(@"|_| \_|\___|\___/ \____\___/|_| |_|_| |_|\___|\___|\__|");            
            Console.WriteLine("");
            Console.WriteLine("Waiting for next scheduled run. Press any key to run immediately.");
            Console.WriteLine("");

            while (!stoppingToken.IsCancellationRequested)
            {
                var utcNow = DateTime.UtcNow;
                var nextUtc = _cron.GetNextOccurrence(utcNow) ?? utcNow;
                                
                //await Task.Delay(nextUtc - utcNow, stoppingToken);                
                await Task.WhenAny(Task.Delay(nextUtc - utcNow, stoppingToken), Task.Run(Console.ReadKey, stoppingToken));

                await _actionsService.PerformActions(stoppingToken);

                Console.WriteLine("");
            }
        }
    }
}
