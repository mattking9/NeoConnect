using System.Text.Json.Serialization;

namespace NeoConnect.Services
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

        [JsonPropertyName("is_day")]
        public int IsDaytime { get; set; }

        [JsonPropertyName("temp_c")]
        public double Temp { get; set; }

        [JsonPropertyName("condition")]
        public ForecastCondition? Condition { get;set;}
    }

    public class ForecastCondition
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("code")]
        public int? Code { get; set; }
    }
}


