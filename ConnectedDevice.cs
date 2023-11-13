using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;

namespace WiFiDirect
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
