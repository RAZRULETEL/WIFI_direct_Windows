using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        public Watcher()
        {

        }

        public bool StartWatching()
        {
            if (deviceWatcher == null)
            {

                discoveredDevices.Clear();
                Debug.WriteLine("Finding Devices...", NotifyType.StatusMessage);

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

            Debug.WriteLine("Device watcher stopped.", NotifyType.StatusMessage);
        }


        #region DeviceWatcherEvents
        private async void OnDeviceAdded(DeviceWatcher deviceWatcher, DeviceInformation deviceInfo)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
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
            Debug.WriteLine("DeviceWatcher enumeration completed", NotifyType.StatusMessage);
        }

        private void OnStopped(DeviceWatcher deviceWatcher, object o)
        {
            Debug.WriteLine("DeviceWatcher stopped", NotifyType.StatusMessage);
        }
        #endregion

        public ObservableCollection<DiscoveredDevice> GetDiscoveredDevices() {
            return discoveredDevices;
        }

        public bool IsWatching()
        {
            return deviceWatcher != null;
        }
    }
}
