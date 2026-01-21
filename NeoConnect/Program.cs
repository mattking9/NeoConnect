using NeoConnect.DataAccess;
using System.Diagnostics;
using System.Net;

namespace NeoConnect
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(@" _   _             ____                            _   ");
            Console.WriteLine(@"| \ | | ___  ___  / ___|___  _ __  _ __   ___  ___| |_ ");
            Console.WriteLine(@"|  \| |/ _ \/ _ \| |   / _ \| '_ \| '_ \ / _ \/ __| __|");
            Console.WriteLine(@"| |\  |  __/ (_) | |__| (_) | | | | | | |  __/ (__| |_ ");
            Console.WriteLine(@"|_| \_|\___|\___/ \____\___/|_| |_|_| |_|\___|\___|\__|");
            Console.WriteLine("");
            
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddLogging(logging =>
                logging.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                })
            );

            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

            builder.Services.AddSingleton<DeviceRepository>();
            builder.Services.AddSingleton<IReportDataService, ReportDataService>();
            builder.Services.AddSingleton<IEmailService, EmailService>();

            builder.Services.AddScoped<IWeatherService, WeatherService>();
            builder.Services.AddScoped<IHeatingService, HeatingService>();
            builder.Services.AddScoped<INeoHubService, NeoHubService>();
            
            builder.Services.AddScoped<ClientWebSocketWrapper>();

            builder.Services.AddSingleton<BathroomBoostAction>();
            builder.Services.AddSingleton<GlobalHoldAction>();
            builder.Services.AddSingleton<ReportDataCollectionAction>();            

            builder.Services.AddHostedService<ScheduledWorker<BathroomBoostAction>>();
            builder.Services.AddHostedService<ScheduledWorker<GlobalHoldAction>>();
            builder.Services.AddHostedService<ScheduledWorker<ReportDataCollectionAction>>();            

            var host = builder.Build();

#if DEBUG     
            using var scope = host.Services.CreateScope();
            string input = null;
            while (input != "s") 
            {
                IScheduledAction action = null;
                Console.WriteLine("Type 's' to run to schedule or enter a number to run one of the following actions:");
                Console.WriteLine("(1) BoostAction");
                Console.WriteLine("(2) HoldAction");
                Console.WriteLine("(3) ReportDataCollectionAction");                
                input = Console.ReadLine();
                switch (input)
                {
                    case "1":
                        action = scope.ServiceProvider.GetRequiredService<BathroomBoostAction>();
                        break;
                    case "2":
                        action = scope.ServiceProvider.GetRequiredService<GlobalHoldAction>();
                        break;
                    case "3":
                        action = scope.ServiceProvider.GetRequiredService<ReportDataCollectionAction>();
                        break;                    
                    default:                        
                        break;
                }
                
                action?.Action(default).GetAwaiter().GetResult();

                Console.WriteLine("");
            }
#endif
            host.Run();

        }
    }
}