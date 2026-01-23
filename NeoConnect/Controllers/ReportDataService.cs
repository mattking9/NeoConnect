using NeoConnect.DataAccess;
using NeoConnect.Data;

namespace NeoConnect
{
    public class ReportDataController
    {
        private readonly DeviceRepository _repo;

        public ReportDataController(DeviceRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<IGrouping<int, DeviceViewModel>>> GetDeviceData(DateTime dateToDisplay)
        {
            var deviceData = await _repo.GetDeviceData(dateToDisplay);

            return deviceData.Select(d => new DeviceViewModel()
            {
                DeviceId = d.DeviceId,
                State = d.PreheatActive ? 2 : d.HeatOn ? 1 : 0,
                Time = d.Timestamp,
                Slice = GetIndex(d.Timestamp)
            }).GroupBy(d => d.DeviceId);
        }

        private int GetIndex(DateTime timestamp)
        {
            return (timestamp.Hour * 4) + (timestamp.Minute < 15 ? 0 : timestamp.Minute < 30 ? 1 : timestamp.Minute < 45 ? 2 : 3);
        }
    }
}
