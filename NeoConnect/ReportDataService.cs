using System.Collections.Concurrent;
using System.Text;

namespace NeoConnect
{
    public class ReportDataService : IReportDataService
    {
        private readonly ConcurrentDictionary<DateTime, IEnumerable<string>> _data;


        public ReportDataService()
        {
            _data = new ConcurrentDictionary<DateTime, IEnumerable<string>>();
        }

        public void Add(IEnumerable<string> data)
        {
            _data.TryAdd(DateTime.Now, data);
        }

        public void Add(string data)
        {
            _data.TryAdd(DateTime.Now, new List<string>([data]));
        }

        public string? ToHtmlReportString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<html>");
            sb.Append("<h1>NeoConnect Report</h1>");

            if (_data.Count == 0)
            {
                sb.Append("<p>Report contains no data</p>");
            }
            else
            {
                foreach (var item in _data.OrderByDescending(d => d.Key))
                {
                    sb.Append($"<h2>{item.Key.ToShortTimeString()}</h2>");
                    sb.Append("<ol>");
                    foreach (var val in item.Value)
                    {
                        sb.Append($"<li>{val}</li>");
                    }
                    sb.Append("</ol>");
                }
            }
            sb.Append("</html>");
            return sb.ToString();
        }

        public void Clear()
        {
            _data?.Clear();
        }
    }
}
