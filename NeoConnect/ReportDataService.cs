using NeoConnect.DataAccess;
using System.Collections.Concurrent;
using System.Text;

namespace NeoConnect
{
    public class ReportDataService : IReportDataService
    {
        private readonly DeviceRepository _deviceRepository;
        private readonly ConcurrentDictionary<DateTime, IEnumerable<string>> _data;


        public ReportDataService(DeviceRepository deviceRepository)
        {
            _deviceRepository = deviceRepository;
            _data = new ConcurrentDictionary<DateTime, IEnumerable<string>>();
        }

        public void AddDeviceData(IEnumerable<NeoDevice> devices, double outsideTemperature)
        {
            var deviceStates = devices.Where(device => device.IsThermostat).Select(device =>
            {
                double setTemp = double.TryParse(device.SetTemp, out double st) ? st : 0.0;
                double actualTemp = double.TryParse(device.ActualTemp, out double at) ? at : 0.0;
                return new DeviceState
                {
                    DeviceId = device.DeviceId,
                    SetTemp = setTemp,
                    ActualTemp = actualTemp,
                    HeatOn = device.IsHeating,
                    PreheatActive = device.IsPreheating,
                    OutsideTemp = outsideTemperature,
                    Timestamp = DateTime.Now
                };
            });            

            _deviceRepository.AddDeviceData(deviceStates);
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
