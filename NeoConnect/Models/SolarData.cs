namespace NeoConnect
{
    public class SolarData
    {
        public DateTime Timestamp { get; set; }
        public decimal GeneratedPower { get; set; }

        public decimal FeedInPower { get; set; }

        public decimal SoC { get; set; }

        public decimal Load { get; set; }

    }
}
