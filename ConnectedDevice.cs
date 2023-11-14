using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;

namespace WiFiDirectApi
{
    public class ConnectedDevice
    {
        public WiFiDirectDevice WfdDevice { get; private set; }
        public DeviceInformation DeviceInfo { get; private set; }

        public ConnectedDevice(WiFiDirectDevice wfdDevice, DeviceInformation deviceInfo)
        {
            WfdDevice = wfdDevice;
            DeviceInfo = deviceInfo;
        }

        public void Dispose()
        {
            // Close WiFiDirectDevice object
            WfdDevice.Dispose();
        }
    }
}
