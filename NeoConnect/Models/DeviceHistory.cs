namespace NeoConnect
{
    public class DeviceHistory
    {
        public string DeviceName { get; set; }

        public int[] History { get; set; } = new int[96];
    }
}
