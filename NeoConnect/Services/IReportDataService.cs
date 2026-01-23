
namespace NeoConnect
{
    public interface IReportDataService
    {
        void AddDeviceData(IEnumerable<NeoDevice> devices, double outsideTemperature);
    }
}