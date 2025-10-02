using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NeoConnect.Services;
using System.Text;

namespace NeoConnect.Tools
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false);

            IConfiguration config = builder.Build();

            var loggerFactory = new LoggerFactory();

            var websocketWrapper = new ClientWebSocketWrapper();

            var neoHubService = new NeoHubService(new Logger<NeoHubService>(loggerFactory), config, websocketWrapper);

            await neoHubService.Connect(CancellationToken.None);
            var profiles = await neoHubService.GetAllProfiles(CancellationToken.None);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<table style=\"width:100%\">");
            sb.AppendLine("<tr><th/><th colspan=\"8\">Weekdays</th><th colspan=\"8\">Weekends</th></tr>");
            sb.AppendLine("<tr><th/><th colspan=\"2\">Wake</th><th colspan=\"2\">Leave</th><th colspan=\"2\">Return</th><th colspan=\"2\">Sleep</th><th colspan=\"2\">Wake</th><th colspan=\"2\">Leave</th><th colspan=\"2\">Return</th><th colspan=\"2\">Sleep</th></tr>");
            foreach (var profile in profiles)
            {
                sb.Append($"<tr><td width=\"20%\">{profile.Value.ProfileName}</th></tr>");

                var schedule = profile.Value.Schedule;
                var comfortLevels = new ComfortLevel[8];
                comfortLevels[0] = new ComfortLevel(schedule.Weekdays.Wake);
                comfortLevels[1] = new ComfortLevel(schedule.Weekdays.Leave);
                comfortLevels[2] = new ComfortLevel(schedule.Weekdays.Return);
                comfortLevels[3] = new ComfortLevel(schedule.Weekdays.Sleep);
                comfortLevels[4] = new ComfortLevel(schedule.Weekdays.Wake);
                comfortLevels[5] = new ComfortLevel(schedule.Weekdays.Leave);
                comfortLevels[6] = new ComfortLevel(schedule.Weekdays.Return);
                comfortLevels[7] = new ComfortLevel(schedule.Weekdays.Sleep);

                foreach(var cl in comfortLevels)
                {
                    sb.Append($"<td width=\"5%\">{cl.Time.ToString(@"HH\:mm")}</td><td width=\"5%\">{cl.TargetTemp}</td>");
                }
                
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");

            var result = sb.ToString();
        }
    }
}
