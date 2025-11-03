using System.Collections.Concurrent;

namespace NeoConnect
{
    public class ReportDataService
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

        public void Clear()
        {
            _data?.Clear();
        }
    }
}
