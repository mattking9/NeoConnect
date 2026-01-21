using NeoConnect.DataAccess;

namespace NeoConnect
{
    public class ReportDataService : IReportDataService
    {
        private readonly DeviceRepository _deviceRepository;

        public ReportDataService(DeviceRepository deviceRepository)
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
    }
}
