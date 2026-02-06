using System.Text.Json.Serialization;

namespace NeoConnect
{
    public class Schedule
    {
        public string ScheduleName { get; set; }        
        public ComfortLevel[] Intervals { get; set; }
    }
}
