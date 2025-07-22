using System;
using System.Diagnostics;
using System.Drawing;

namespace NeoConnect
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddLogging(logging =>
                logging.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                })
            );            

            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

            builder.Services.Configure<HeatingConfig>(builder.Configuration.GetSection("HeatingConfig"));

            builder.Services.AddSingleton<IWeatherService, WeatherService>();
            builder.Services.AddSingleton<IHeatingService, HeatingService>();
            builder.Services.AddSingleton<INeoHubService, NeoHubService>();            
            builder.Services.AddSingleton<IEmailService, EmailService>();

            builder.Services.AddSingleton<ActionsService>();

            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}