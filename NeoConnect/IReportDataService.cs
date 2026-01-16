
namespace NeoConnect
{
    public interface IReportDataService
    {
        void Add(IEnumerable<string> data);
        void Add(string data);
        void AddDeviceData(IEnumerable<NeoDevice> devices, double outsideTemperature);
        void Clear();
        string? ToHtmlReportString();
    }
}