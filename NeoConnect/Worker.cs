using Cronos;
using Microsoft.Extensions.DependencyInjection;

namespace NeoConnect
{
    public class Worker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _config;

        public Worker(IConfiguration config, IServiceScopeFactory serviceScopeFactory)
        {
            _config = config;
            _serviceScopeFactory = serviceScopeFactory;
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

            // If a schedule is defined then run to that schedule, otherwise run once
            if (schedule != null && CronExpression.TryParse(schedule, CronFormat.Standard, out _cron))
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var utcNow = DateTime.UtcNow;
                    var nextRunUtc = _cron.GetNextOccurrence(utcNow) ?? utcNow;

                    Console.WriteLine("Next run scheduled for " + nextRunUtc.ToLocalTime().ToString("G"));
                    await Task.Delay(nextRunUtc - utcNow, stoppingToken);

                    using var scope = _serviceScopeFactory.CreateScope();
                    var actionsService = scope.ServiceProvider.GetRequiredService<ActionsService>();
                    await actionsService.PerformActions(stoppingToken);

                    Console.WriteLine("");
                }
            }
            else
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var actionsService = scope.ServiceProvider.GetRequiredService<ActionsService>();
                await actionsService.PerformActions(stoppingToken);

                Environment.Exit(0);
            }
        }
    }
}
