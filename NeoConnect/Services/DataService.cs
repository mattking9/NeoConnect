using NeoConnect.DataAccess;

namespace NeoConnect
{
    public class DataService : IDataService
    {
        private readonly IDeviceRepository _deviceRepository;        

        public DataService(IDeviceRepository deviceRepository)
        {
            _deviceRepository = deviceRepository;
        }

        public void RefreshDeviceList(IEnumerable<Device> devices)
        {
            _deviceRepository.AddDevices(devices.Select(d => new DeviceEntity() { DeviceId = d.DeviceId, DeviceName = d.ZoneName }));
        }

        public void AddDeviceData(IEnumerable<Device> devices, double outsideTemperature)
        {            
            var deviceStates = new List<DeviceStateEntity>(capacity: devices.Count());

            foreach (var device in devices)
            {
                double setTemp = device.SetTemp;
                double actualTemp = device.ActualTemp;

                deviceStates.Add(new DeviceStateEntity
                {
                    DeviceId = device.DeviceId,
                    SetTemp = setTemp,
                    ActualTemp = actualTemp,
                    HeatOn = device.IsHeating || device.TimerOn,
                    PreheatActive = device.IsPreheating,
                    OutsideTemp = outsideTemperature,
                    Timestamp = DateTime.UtcNow // Use UTC for consistency
                });
            }

            _deviceRepository.AddDeviceData(deviceStates);
        }

        public async Task<IEnumerable<DeviceStateEntity>> GetDeviceData(DateTime dateToDisplay)
        {
            return await _deviceRepository.GetDeviceData(dateToDisplay);            
        }        
    }
}
