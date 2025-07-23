using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NeoConnect
{
    public class NeoHubResponse
    {
        [JsonPropertyName("command_id")]
        public int CommandId { get; set; }

        [JsonPropertyName("device_id")]
        public string DeviceId { get; set; }

        [JsonPropertyName("message_type")]
        public string MessageType { get; set; }

        [JsonPropertyName("response")]
        public string ResponseJson { get; set; }
    }

    public class NeoHubLiveData
    {
        [JsonPropertyName("devices")]
        public List<NeoDevice> Devices { get; set; } = new List<NeoDevice>();
    }

    public class NeoDevice
    {
        [JsonPropertyName("ACTIVE_PROFILE")]
        public int ActiveProfile { get; set; }

        [JsonPropertyName("ACTUAL_TEMP")]
        public string ActualTemp { get; set; }

        [JsonPropertyName("SET_TEMP")]
        public string TargetTemp { get; set; }

        [JsonPropertyName("ZONE_NAME")]
        public string ZoneName { get; set; }

        [JsonPropertyName("THERMOSTAT")]
        public bool IsThermostat { get; set; }

        [JsonPropertyName("OFFLINE")]
        public bool IsOffline { get; set; }
    }

    public class Profile
    {
        [JsonPropertyName("PROFILE_ID")]
        public int ProfileId { get; set; }

        [JsonPropertyName("name")]
        public string ProfileName { get; set; }

        [JsonPropertyName("info")]
        public ProfileSchedule Schedule { get; set; }
    }

    public class ProfileSchedule
    {
        [JsonPropertyName("monday")]
        public ProfileScheduleGroup Weekdays { get; set; }

        [JsonPropertyName("sunday")]
        public ProfileScheduleGroup Weekends { get; set; }              
    }

    public class ProfileScheduleGroup
    {
        [JsonPropertyName("leave")]
        public object[] Leave { get; set; }

        [JsonPropertyName("return")]
        public object[] Return { get; set; }

        [JsonPropertyName("sleep")]
        public object[] Sleep { get; set; }

        [JsonPropertyName("wake")]
        public object[] Wake { get; set; }

        private IEnumerable<ComfortLevel> ToComfortLevels() => 
            new ComfortLevel[]
            {
                new ComfortLevel(Wake),
                new ComfortLevel(Leave),
                new ComfortLevel(Return),
                new ComfortLevel(Sleep)
            }.OrderBy(i => i.Time);
        
        
    }

    public class EngineersData
    {
        [JsonPropertyName("DEVICE_ID")]
        public int DeviceId { get; set; }

        [JsonPropertyName("DEVICE_TYPE")]
        public int DeviceType { get; set; }

        [JsonPropertyName("MAX_PREHEAT")]
        public int MaxPreheatDuration { get; set; }       
    }
}


