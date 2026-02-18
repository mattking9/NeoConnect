namespace NeoConnect.DataAccess
{
    public interface IDeviceRepository
    {
        void AddDeviceData(IEnumerable<DeviceStateEntity> deviceStates);
        void AddDevices(IEnumerable<DeviceEntity> devices);
        Task<IEnumerable<DeviceStateEntity>> GetDeviceData(DateTime dateToDisplay);
    }
}