namespace NeoConnect
{
    public class ComfortLevel
    {
        public ComfortLevel(object[] interval)
        {
            if (interval != null && interval.Length >= 2)
            {
                Time = TimeOnly.Parse(interval[0].ToString());
                TargetTemp = decimal.Parse(interval[1].ToString());
            }
        }

        public TimeOnly Time { get; private set; }
        public decimal TargetTemp { get; private set; }
    }
}


