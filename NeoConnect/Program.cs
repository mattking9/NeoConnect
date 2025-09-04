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

            builder.Services.AddSystemd();

            builder.Services.AddLogging(logging =>
                logging.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                })
            );

            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

            builder.Services.Configure<HeatingConfig>(builder.Configuration.GetSection("HeatingConfig"));

            builder.Services.AddScoped<IWeatherService, WeatherService>();
            builder.Services.AddScoped<IHeatingService, HeatingService>();
            builder.Services.AddScoped<INeoHubService, NeoHubService>();            
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<ActionsService>();

            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}