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

            builder.Services.AddSystemd();

            builder.Services.AddLogging(logging => logging.AddSystemdConsole());

            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

            builder.Services.Configure<HeatingConfig>(builder.Configuration.GetSection("HeatingConfig"));

            builder.Services.AddScoped<IWeatherService, WeatherService>();
            builder.Services.AddScoped<IHeatingService, HeatingService>();
            builder.Services.AddScoped<INeoHubService, NeoHubService>();            
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<ClientWebSocketWrapper>();
            builder.Services.AddScoped<ActionsService>();            

            builder.Services.AddHostedService<Worker>();            

            var host = builder.Build();
                        
            var config = host.Services.GetRequiredService<IConfiguration>();
            if (!string.IsNullOrWhiteSpace(config["Schedule"]))
            {
                // If a schedule is defined then run worker as a background service.
                host.Run();                
            }
            else
            {
                // Otherwise, run once and exit
                using var scope = host.Services.CreateScope();
                var actions = scope.ServiceProvider.GetRequiredService<ActionsService>();               
                actions.PerformActions(default).GetAwaiter().GetResult();
            }
        }
    }
}