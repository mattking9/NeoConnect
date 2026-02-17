namespace NeoConnect.DataAccess
{
    public class DeviceStateEntity
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; }
        public double SetTemp { get; set; }
        public double ActualTemp { get; set; }
        public bool HeatOn { get; set; }
        public bool PreheatActive { get; set; }
        public double OutsideTemp { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
