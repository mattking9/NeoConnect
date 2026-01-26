
using NeoConnect.DataAccess;

namespace NeoConnect
{
    public interface IReportDataService
    {
        void AddDeviceData(IEnumerable<NeoDevice> devices, double outsideTemperature);
        Task<IEnumerable<DeviceState>> GetDeviceData(DateTime dateToDisplay);
    }
}