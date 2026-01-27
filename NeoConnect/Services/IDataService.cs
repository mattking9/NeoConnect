
using NeoConnect.DataAccess;

namespace NeoConnect
{
    public interface IDataService
    {
        void AddDeviceData(IEnumerable<NeoDevice> devices, double outsideTemperature);
        void CacheDeviceNames(Dictionary<int, string> deviceNames);
        void CacheProfileNames(Dictionary<int, string> profileNames);
        Task<IEnumerable<DeviceState>> GetDeviceData(DateTime dateToDisplay);
        string GetDeviceName(int deviceId);
        string GetProfileName(int profileId);
    }
}