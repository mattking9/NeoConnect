using System.Text.Json.Serialization;

namespace NeoConnect
{
    public class WeatherResponse
    {
        [JsonPropertyName("forecast")]
        public Forecast Forecast { get; set; }
    }

    public class Forecast
    {
        [JsonPropertyName("forecastday")]
        public List<ForecastDay> ForecastDay { get; set; } = new List<ForecastDay>();
    }

    public class ForecastDay
    {
        [JsonPropertyName("day")]
        public ForecastDayDaily Day { get; set; }

        [JsonPropertyName("hour")]
        public List<ForecastHour> Hour { get; set; } = new List<ForecastHour>();
    }

    public class ForecastDayDaily
    {
        [JsonPropertyName("avgtemp_c")]
        public double? AverageTemp { get; set; }
    }

    public class ForecastHour
    {        
        public string? Time { get; set; }

        [JsonPropertyName("temp_c")]
        public double Temp { get; set; }
    }
}


