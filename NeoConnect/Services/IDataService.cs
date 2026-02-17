
using NeoConnect.DataAccess;

namespace NeoConnect
{
    public interface IDataService
    {
        void AddDeviceData(IEnumerable<NeoDevice> devices, double outsideTemperature);        
        Task<IEnumerable<DeviceStateEntity>> GetDeviceData(DateTime dateToDisplay);
        void RefreshDeviceList(IEnumerable<NeoDevice> devices);
    }
}