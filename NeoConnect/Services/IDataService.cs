
using NeoConnect.DataAccess;

namespace NeoConnect
{
    public interface IDataService
    {
        void AddDeviceData(IEnumerable<NeoDevice> devices, double outsideTemperature);        
        Task<IEnumerable<DeviceState>> GetDeviceData(DateTime dateToDisplay);        
    }
}