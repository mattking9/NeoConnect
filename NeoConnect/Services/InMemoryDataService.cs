namespace NeoConnect
{
    public class InMemoryDataService
    {
        private Dictionary<int, string> _deviceNameCache = new Dictionary<int, string>();
        private Dictionary<int, string> _profileNameCache = new Dictionary<int, string>();

        public void CacheDeviceNames(Dictionary<int, string> deviceNames)
        {
            _deviceNameCache = deviceNames;
        }

        public string GetDeviceName(int deviceId)
        {
            if (_deviceNameCache.TryGetValue(deviceId, out string name) && name != null)
            {
                return name;
            }

            return "Device " + deviceId;
        }

        public void CacheProfileNames(Dictionary<int, string> profileNames)
        {
            _profileNameCache = profileNames;
        }

        public string GetProfileName(int profileId)
        {
            if (_profileNameCache.TryGetValue(profileId, out string name) && name != null)
            {
                return name;
            }

            return "Profile " + profileId;
        }
    }
}
