namespace NeoConnect
{
    public class ComfortLevel
    {
        public ComfortLevel(object[] interval, bool isWake = false)
        {
            if (interval != null && interval.Length >= 2)
            {
                Time = TimeOnly.Parse(interval[0].ToString());
                TargetTemp = decimal.Parse(interval[1].ToString());
                IsWake = isWake;
            }
        }

        public TimeOnly Time { get; private set; }
        public decimal TargetTemp { get; private set; }
        public bool IsWake { get; private set; }
    }
}


