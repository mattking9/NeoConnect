using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class SolarForecastResponse
{
    [JsonPropertyName("result")]
    public ForecastResult Result { get; set; }

    [JsonPropertyName("message")]
    public ForecastMessage Message { get; set; }
}

public class ForecastResult
{
    // Total watts expected at specific hourly timestamps (Key: "YYYY-MM-DD HH:mm:ss")
    [JsonPropertyName("watts")]
    public Dictionary<string, int> Watts { get; set; }

    // Cumulative Watt-hours generated throughout the day up to that specific hour
    [JsonPropertyName("watt_hours")]
    public Dictionary<string, int> WattHours { get; set; }

    // Total estimated Watt-hours for the entire day (Key: "YYYY-MM-DD")
    [JsonPropertyName("watt_hours_day")]
    public Dictionary<string, int> WattHoursDay { get; set; }

    // Estimated daily total Watt-hours assuming completely clear sky conditions
    [JsonPropertyName("watt_hours_period")]
    public Dictionary<string, int> WattHoursPeriod { get; set; }
}

public class ForecastMessage
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("info")]
    public ForecastInfo Info { get; set; }
}

public class ForecastInfo
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("distance")]
    public int Distance { get; set; }

    [JsonPropertyName("place")]
    public string Place { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; }

    [JsonPropertyName("time")]
    public DateTime Time { get; set; }
}