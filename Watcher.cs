using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using SDKTemplate;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;
using Windows.UI.Core;

namespace WiFiDirectApi
{
    public class Watcher
    {
        private DeviceWatcher deviceWatcher = null;

        private ObservableCollection<DiscoveredDevice> discoveredDevices { get; } = new ObservableCollection<DiscoveredDevice>();

        Advertiser advertiser = new Advertiser();

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

        public void StopWatching()
        {
            deviceWatcher.Added -= OnDeviceAdded;
            deviceWatcher.Removed -= OnDeviceRemoved;
            deviceWatcher.Updated -= OnDeviceUpdated;
            deviceWatcher.EnumerationCompleted -= OnEnumerationCompleted;
            deviceWatcher.Stopped -= OnStopped;

            deviceWatcher.Stop();

            deviceWatcher = null;

            advertiser.StopAdvertisement();

            Debug.WriteLine("Device watcher stopped." );
        }


        #region DeviceWatcherEvents
        private async void OnDeviceAdded(DeviceWatcher deviceWatcher, DeviceInformation deviceInfo)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Debug.WriteLine("New device found: " + deviceInfo.Name);
                discoveredDevices.Add(new DiscoveredDevice(deviceInfo));
            });
        }

        private async void OnDeviceRemoved(DeviceWatcher deviceWatcher, DeviceInformationUpdate deviceInfoUpdate)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (DiscoveredDevice discoveredDevice in discoveredDevices)
                {
                    if (discoveredDevice.DeviceInfo.Id == deviceInfoUpdate.Id)
                    {
                        discoveredDevices.Remove(discoveredDevice);
                        break;
                    }
                }
            });
        }

        private async void OnDeviceUpdated(DeviceWatcher deviceWatcher, DeviceInformationUpdate deviceInfoUpdate)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (DiscoveredDevice discoveredDevice in discoveredDevices)
                {
                    if (discoveredDevice.DeviceInfo.Id == deviceInfoUpdate.Id)
                    {
                        discoveredDevice.UpdateDeviceInfo(deviceInfoUpdate);
                        break;
                    }
                }
            });
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

        public ObservableCollection<DiscoveredDevice> GetDiscoveredDevices() {
            return discoveredDevices;
        }

        public bool IsWatching()
        {
            return deviceWatcher != null;
        }

        public void SendMessage(String msg)
        {
            advertiser.SendMessage(msg);
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

            // Register for the ConnectionStatusChanged event handler
            wfdDevice.ConnectionStatusChanged += OnConnectionStatusChanged;

            await advertiser.StartSocketListener(wfdDevice);
            advertiser.RequestSocketTransfer(wfdDevice);
        }

        private void OnConnectionStatusChanged(WiFiDirectDevice sender, object arg)
        {
            Debug.WriteLine($"Connection status changed: {sender.ConnectionStatus}" );
        }
    }
}
