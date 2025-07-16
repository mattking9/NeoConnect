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

            builder.Services.Configure<ActionsConfig>(builder.Configuration.GetSection("Actions"));

            builder.Services.AddSingleton<WeatherService>();
            builder.Services.AddSingleton<NeoHubService>();
            builder.Services.AddSingleton<ActionsService>();
            builder.Services.AddSingleton<EmailService>();

            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}