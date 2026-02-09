
namespace NeoConnect
{    
    public class Device
    {
        public int DeviceId { get; set; }        
        public string ProfileName { get; set; }
        public string ActualTemp { get; set; }        
        public string SetTemp { get; set; }        
        public string ZoneName { get; set; }      
        public bool IsThermostat { get; set; }        
        public bool IsOffline { get; set; }        
        public bool IsStandby { get; set; }        
        public bool IsHeating { get; set; }        
        public bool IsPreheating { get; set; }        
        public bool TimerOn { get; set; }
        public int? RoC { get; set; }
        public int? MaxPreheatHours { get; set; }

        public static Device FromNeoDevice(NeoDevice neoDevice)
        {
            return new Device()
            {
                DeviceId = neoDevice.DeviceId,
                ActualTemp = neoDevice.ActualTemp,
                IsHeating = neoDevice.IsHeating,
                IsOffline = neoDevice.IsOffline,
                IsPreheating = neoDevice.IsPreheating,
                IsStandby = neoDevice.IsStandby,
                IsThermostat = neoDevice.IsThermostat,
                SetTemp = neoDevice.SetTemp,
                TimerOn = neoDevice.TimerOn,
                ZoneName = neoDevice.ZoneName
            };
        }
    }
}
