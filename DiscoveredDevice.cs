using Windows.Devices.Enumeration;

namespace WiFiDirectApi
{
    public class DiscoveredDevice
    {
        public DeviceInformation DeviceInfo { get; private set; }

        public DiscoveredDevice(DeviceInformation deviceInfo)
        {
            DeviceInfo = deviceInfo;
        }

        public string DisplayName => DeviceInfo.Name;// + " - " + (DeviceInfo.Pairing.IsPaired ? "Paired" : "Unpaired");

        public override string ToString() => DeviceInfo.Id + "🔹" + DeviceInfo.Name;
        public void UpdateDeviceInfo(DeviceInformationUpdate update)
        {
            DeviceInfo.Update(update);
        }
    }
}
