// Use source generators for better performance and lower memory usage

using System.Text.Json.Serialization;

namespace NeoConnect.Services
{
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(HeatingConfig))]
    [JsonSerializable(typeof(WeatherResponse))]
    [JsonSerializable(typeof(NeoHubResponse))]
    [JsonSerializable(typeof(NeoHubLiveData))]
    [JsonSerializable(typeof(Dictionary<string, EngineersData>))]
    [JsonSerializable(typeof(Dictionary<string, Profile>))]
    [JsonSerializable(typeof(Dictionary<string, int>))]
    internal partial class NeoConnectJsonContext : JsonSerializerContext
    {
    }
}