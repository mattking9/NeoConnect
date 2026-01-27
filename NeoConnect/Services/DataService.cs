using NeoConnect.DataAccess;

namespace NeoConnect
{
    public class DataService : IDataService
    {
        private readonly DeviceRepository _deviceRepository;

        private Dictionary<int, string> _deviceNameCache = new Dictionary<int, string>();
        private Dictionary<int, string> _profileNameCache = new Dictionary<int, string>();

        public DataService(DeviceRepository deviceRepository)
        {
            _deviceRepository = deviceRepository;
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

        public async Task<IEnumerable<DeviceState>> GetDeviceData(DateTime dateToDisplay)
        {
            return await _deviceRepository.GetDeviceData(dateToDisplay);            
        }

        public void CacheDeviceNames(Dictionary<int, string> deviceNames)
        {
            _deviceNameCache = deviceNames;
        }

        public string GetDeviceName(int deviceId)
        {
            if (_deviceNameCache.TryGetValue(deviceId, out string name) && name != null)
            {
                return name;
            }

            return "Device " + deviceId;
        }

        public void CacheProfileNames(Dictionary<int, string> profileNames)
        {
            _profileNameCache = profileNames;
        }

        public string GetProfileName(int profileId)
        {
            if (_profileNameCache.TryGetValue(profileId, out string name) && name != null)
            {
                return name;
            }

            return "Profile " + profileId;
        }
    }
}
