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

        public void RefreshDeviceList(IEnumerable<NeoDevice> devices)
        {
            _deviceRepository.AddDevices(devices.Select(d => new DeviceEntity() { DeviceId = d.DeviceId, DeviceName = d.ZoneName }));
        }

        public void AddDeviceData(IEnumerable<NeoDevice> devices, double outsideTemperature)
        {
            var deviceList = devices as IList<NeoDevice> ?? devices.ToList();
            var deviceStates = new List<DeviceStateEntity>(capacity: deviceList.Count);

            foreach (var device in deviceList)
            {
                double setTemp = double.TryParse(device.SetTemp, out double st) ? st : 0.0;
                double actualTemp = double.TryParse(device.ActualTemp, out double at) ? at : 0.0;

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
