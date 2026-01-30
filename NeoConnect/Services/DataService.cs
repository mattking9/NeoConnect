using NeoConnect.DataAccess;

namespace NeoConnect
{

    public class DataService : IDataService
    {
        private readonly DeviceRepository _deviceRepository;        

        public DataService(DeviceRepository deviceRepository)
        {
            _deviceRepository = deviceRepository;
        }

        public void AddDeviceData(IEnumerable<NeoDevice> devices, double outsideTemperature)
        {
            var deviceList = devices as IList<NeoDevice> ?? devices.ToList();
            var deviceStates = new List<DeviceState>(capacity: deviceList.Count);

            foreach (var device in deviceList)
            {
                if (!device.IsThermostat) continue;

                double setTemp = double.TryParse(device.SetTemp, out double st) ? st : 0.0;
                double actualTemp = double.TryParse(device.ActualTemp, out double at) ? at : 0.0;

                deviceStates.Add(new DeviceState
                {
                    DeviceId = device.DeviceId,
                    SetTemp = setTemp,
                    ActualTemp = actualTemp,
                    HeatOn = device.IsHeating,
                    PreheatActive = device.IsPreheating,
                    OutsideTemp = outsideTemperature,
                    Timestamp = DateTime.UtcNow // Use UTC for consistency
                });
            }

            _deviceRepository.AddDeviceData(deviceStates);
        }

        public async Task<IEnumerable<DeviceState>> GetDeviceData(DateTime dateToDisplay)
        {
            return await _deviceRepository.GetDeviceData(dateToDisplay);            
        }        
    }
}
