namespace NeoConnect
{
    public class ComfortLevel
    {
        public ComfortLevel(object[] interval)
        {
            if (interval != null && interval.Length >= 2)
            {
                Time = TimeOnly.Parse(interval[0].ToString());
                TargetTemp = double.Parse(interval[1].ToString());
            }
        }

        public TimeOnly Time { get; private set; }
        public double TargetTemp { get; private set; }
    }
}


