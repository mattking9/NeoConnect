using System.Text.Json.Serialization;

namespace NeoConnect
{
    public class WeatherResponse
    {
        public Forecast Forecast { get; set; }
    }

    public class Forecast
    {
        public List<ForecastDay> ForecastDay { get; set; } = new List<ForecastDay>();
    }

    public class ForecastDay
    {
        public DateOnly Date { get; set; }
        public ForecastDayDaily Day { get; set; }

        public List<ForecastHour> Hour { get; set; } = new List<ForecastHour>();        

    }

    public class ForecastDayDaily
    {
        [JsonPropertyName("avgtemp_c")]
        public decimal? AverageTemp { get; set; }
    }

    public class ForecastHour
    {        
        public string? Time { get; set; }

        [JsonPropertyName("Temp_C")]
        public decimal? Temp { get; set; }
        public ForecastCondition? Condition { get;set;}
    }

    public class ForecastCondition
    {
        public string? Text { get; set; }
        public string? Icon { get; set; }
    }
}


