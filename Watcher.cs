using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;

namespace WiFiDirectApi
{
    public class Watcher
    {
        private DeviceWatcher deviceWatcher = null;

        private List<DiscoveredDevice> discoveredDevices { get; } = new List<DiscoveredDevice>();

        public Advertiser advertiser { get; } = new Advertiser();

        public Watcher()
        {

        }

        public bool StartWatching()
        {
            if (deviceWatcher == null)
            {

                advertiser.StartAdvertisement();

                discoveredDevices.Clear();
                Debug.WriteLine("Finding Devices..." );

                String deviceSelector = WiFiDirectDevice.GetDeviceSelector(WiFiDirectDeviceSelectorType.AssociationEndpoint);

                deviceWatcher = DeviceInformation.CreateWatcher(deviceSelector, new string[] { "System.Devices.WiFiDirect.InformationElements" });

                deviceWatcher.Added += OnDeviceAdded;
                deviceWatcher.Removed += OnDeviceRemoved;
                deviceWatcher.Updated += OnDeviceUpdated;
                deviceWatcher.EnumerationCompleted += OnEnumerationCompleted;
                deviceWatcher.Stopped += OnStopped;

                deviceWatcher.Start();

                return deviceWatcher.Status == DeviceWatcherStatus.Started;
            }
            return false;
        }

        public bool StopWatching()
        {
            deviceWatcher.Added -= OnDeviceAdded;
            deviceWatcher.Removed -= OnDeviceRemoved;
            deviceWatcher.Updated -= OnDeviceUpdated;
            deviceWatcher.EnumerationCompleted -= OnEnumerationCompleted;
            deviceWatcher.Stopped -= OnStopped;

            deviceWatcher.Stop();

            advertiser.StopAdvertisement();

            Debug.WriteLine("Device watcher stopped." );

            var status = deviceWatcher.Status;
            deviceWatcher = null;

            return status == DeviceWatcherStatus.Stopped || status == DeviceWatcherStatus.Stopping;
        }


        #region DeviceWatcherEvents
        private void OnDeviceAdded(DeviceWatcher deviceWatcher, DeviceInformation deviceInfo)
        {
            Debug.WriteLine("New device found: " + deviceInfo.Name);
            discoveredDevices.Add(new DiscoveredDevice(deviceInfo));
        }

        private void OnDeviceRemoved(DeviceWatcher deviceWatcher, DeviceInformationUpdate deviceInfoUpdate)
        {
            foreach (DiscoveredDevice discoveredDevice in discoveredDevices)
             {
                if (discoveredDevice.DeviceInfo.Id == deviceInfoUpdate.Id)
                {
                    discoveredDevices.Remove(discoveredDevice);
                    break;
                }
            }
        }

        private void OnDeviceUpdated(DeviceWatcher deviceWatcher, DeviceInformationUpdate deviceInfoUpdate)
        {
            foreach (DiscoveredDevice discoveredDevice in discoveredDevices)
            {
                if (discoveredDevice.DeviceInfo.Id == deviceInfoUpdate.Id)
                {
                    discoveredDevice.UpdateDeviceInfo(deviceInfoUpdate);
                    break;
                }
            }
        }

        private void OnEnumerationCompleted(DeviceWatcher deviceWatcher, object o)
        {
            Debug.WriteLine("DeviceWatcher enumeration completed" );
        }

        private void OnStopped(DeviceWatcher deviceWatcher, object o)
        {
            Debug.WriteLine("DeviceWatcher stopped" );
        }
        #endregion

        public DiscoveredDevice[] GetDiscoveredDevices() {
            return discoveredDevices.ToArray();
        }

        public async void ConnectDevice(DiscoveredDevice discoveredDevice)
        {
            if (discoveredDevice == null)
            {
                Debug.WriteLine("No device selected, please select one." );
                return;
            }

            Debug.WriteLine($"Connecting to {discoveredDevice.DeviceInfo.Name}..." );

            if (!discoveredDevice.DeviceInfo.Pairing.IsPaired)
            {
                if (!await Advertiser.RequestPairDeviceAsync(discoveredDevice.DeviceInfo.Pairing))
                {
                    return;
                }
            }

            WiFiDirectDevice wfdDevice = null;
            try
            {
                // IMPORTANT: FromIdAsync needs to be called from the UI thread
                wfdDevice = await WiFiDirectDevice.FromIdAsync(discoveredDevice.DeviceInfo.Id);
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("FromIdAsync was canceled by user" );
                return;
            }

            wfdDevice.ConnectionStatusChanged += OnConnectionStatusChanged;
            advertiser.connectedDevices.Add(new ConnectedDevice(wfdDevice, discoveredDevice.DeviceInfo));
        }

        private void OnConnectionStatusChanged(WiFiDirectDevice wfdDevice, object arg)
        {
            Debug.WriteLine($"Connection status changed: {wfdDevice.ConnectionStatus}" );
            foreach (ConnectedDevice dev in advertiser.connectedDevices)
            {
                if (dev.WfdDevice.DeviceId == wfdDevice.DeviceId)
                {
                    _ = dev.DeviceInfo.Pairing.UnpairAsync();
                    advertiser.connectedDevices.Remove(dev);
                    break;
                }
            }
        }
    }
}
