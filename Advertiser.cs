using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;
using Windows.Networking;

namespace WiFiDirectApi
{

    public class Advertiser
    {

        private WiFiDirectAdvertisementPublisher publisher;
        private WiFiDirectConnectionListener listener;
        internal HashSet<ConnectedDevice> connectedDevices = new HashSet<ConnectedDevice>();

        
        public Advertiser()
        {

        }


        public bool StartAdvertisement()
        {
            publisher = new WiFiDirectAdvertisementPublisher();
            publisher.StatusChanged += OnStatusChanged;

            listener = new WiFiDirectConnectionListener();

            var discoverability = WiFiDirectAdvertisementListenStateDiscoverability.Intensive;
            publisher.Advertisement.ListenStateDiscoverability = discoverability;

            publisher.Advertisement.IsAutonomousGroupOwnerEnabled = true;

            try
            {
                // This can raise an exception if the machine does not support WiFi. Sorry.
                listener.ConnectionRequested += OnConnectionRequested;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error preparing Advertisement: {ex}" );
                return false;
            }

            publisher.Start();

            return publisher.Status == WiFiDirectAdvertisementPublisherStatus.Started;
        }

        public bool StopAdvertisement()
        {
            if (publisher.Status != WiFiDirectAdvertisementPublisherStatus.Aborted
                && publisher.Status != WiFiDirectAdvertisementPublisherStatus.Stopped)
            {
                publisher.Stop();

                publisher.StatusChanged -= OnStatusChanged;

                listener.ConnectionRequested -= OnConnectionRequested;
                return true;
            }
            return false;
        }

        private void OnStatusChanged(WiFiDirectAdvertisementPublisher sender, WiFiDirectAdvertisementPublisherStatusChangedEventArgs statusEventArgs)
        {
            Debug.WriteLine($"Advertiser status changed: ${statusEventArgs.Status}");
        }
        private WiFiDirectConnectionRequest req;
        private void OnConnectionRequested(WiFiDirectConnectionListener sender, WiFiDirectConnectionRequestedEventArgs connectionEventArgs)
        {
            WiFiDirectConnectionRequest connectionRequest = connectionEventArgs.GetConnectionRequest();
            req = connectionRequest;
            ThreadPool.QueueUserWorkItem(new WaitCallback(handleReq));
        }

        private async void handleReq(object obj) {
            Debug.WriteLine($"Connection requested from ${req.DeviceInformation.Name}!!!");
            if (!await HandleConnectionRequestAsync(req))
            {
                Debug.WriteLine("Pair request was declined !!!");
                req.Dispose();
            }   
        }


        private async Task<bool> HandleConnectionRequestAsync(WiFiDirectConnectionRequest connectionRequest)
        {
            string deviceName = connectionRequest.DeviceInformation.Name;

            bool isPaired = (connectionRequest.DeviceInformation.Pairing?.IsPaired == true) ||
                            (await IsAepPairedAsync(connectionRequest.DeviceInformation.Id));

            Debug.WriteLine($"Connecting to {deviceName}..." );

            // Show the prompt only in case of WiFiDirect reconnection or Legacy client connection.
            if (isPaired || publisher.Advertisement.LegacySettings.IsEnabled)
            {
                Debug.WriteLineIf(isPaired, "Reconnect!");
                Debug.WriteLineIf(publisher.Advertisement.LegacySettings.IsEnabled, "Legacy connection!");

                WiFiDirectDevice wfdDevice1 = null;
                try
                {
                    wfdDevice1 = await WiFiDirectDevice.FromIdAsync(connectionRequest.DeviceInformation.Id);

                    Debug.WriteLine($"Reconnect from ${wfdDevice1.DeviceId}");

                    connectedDevices.Add(new ConnectedDevice(wfdDevice1, connectionRequest.DeviceInformation));

                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception in FromIdAsync: {ex}");
                }


                await connectionRequest.DeviceInformation.Pairing.UnpairAsync();
                isPaired = false;
            }

            // Pair device if not already paired and not using legacy settings
            if (!isPaired && !publisher.Advertisement.LegacySettings.IsEnabled)
            {
                Debug.WriteLine("Pairing: " + connectionRequest.DeviceInformation.Name);
                if (!await RequestPairDeviceAsync(connectionRequest.DeviceInformation.Pairing, false))
                {
                    Debug.WriteLine("Pair request accept fail");
                    return false;
                }
            }
            Debug.WriteLine("Device info: " + connectionRequest.DeviceInformation.Properties.Keys.ToString());
            WiFiDirectDevice wfdDevice = null;

            try
            {
                // IMPORTANT: FromIdAsync needs to be called from the UI thread
                wfdDevice = await WiFiDirectDevice.FromIdAsync(connectionRequest.DeviceInformation.Id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in FromIdAsync: {ex}");
                return false;
            }

            wfdDevice.ConnectionStatusChanged += OnConnectionStatusChanged;
            connectedDevices.Add(new ConnectedDevice(wfdDevice, connectionRequest.DeviceInformation));
            return true;
        }

        static public async Task<bool> RequestPairDeviceAsync(DeviceInformationPairing pairing, bool isConnectRequest = true)
        {
            Debug.WriteLine("Trying pair device async");
            WiFiDirectConnectionParameters connectionParams = new WiFiDirectConnectionParameters();
            connectionParams.GroupOwnerIntent = (short)(15 - (isConnectRequest ? 1 : 0));


            DevicePairingKinds devicePairingKinds = DevicePairingKinds.ConfirmOnly;// | DevicePairingKinds.ConfirmPinMatch
            //    | DevicePairingKinds.DisplayPin;


            connectionParams.PreferredPairingProcedure = WiFiDirectPairingProcedure.GroupOwnerNegotiation;
            DeviceInformationCustomPairing customPairing = pairing.Custom;
            customPairing.PairingRequested += OnPairingRequested;


            DevicePairingResult result = await customPairing.PairAsync(devicePairingKinds, DevicePairingProtectionLevel.None, connectionParams);
            Debug.WriteLine("Pair async exec");
            if (result.Status != DevicePairingResultStatus.Paired)
            {
                Debug.WriteLine($"PairAsync failed, Status: {result.Status}" );
                if(result.Status == DevicePairingResultStatus.AlreadyPaired)
                {
                    await pairing.UnpairAsync();
                    return await RequestPairDeviceAsync(pairing, isConnectRequest);
                }
                return false;
            }
            return true;
        }

        static private void OnPairingRequested(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            Debug.WriteLine("On pairing request " + args.PairingKind);// Works only with ConfirmOnly
            args.Accept();// TODO: implement more kinds
            //Utils.HandlePairing(System.Windows.Threading.Dispatcher.CurrentDispatcher, args);
            
        }
        private async Task<bool> IsAepPairedAsync(string deviceId)
        {
            List<string> additionalProperties = new List<string>();
            additionalProperties.Add("System.Devices.Aep.DeviceAddress");
            String deviceSelector = $"System.Devices.Aep.AepId:=\"{deviceId}\"";
            DeviceInformation devInfo = null;

            try
            {
                devInfo = await DeviceInformation.CreateFromIdAsync(deviceId, additionalProperties);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DeviceInformation.CreateFromIdAsync threw an exception: " + ex.Message );
            }

            if (devInfo == null)
            {
                Debug.WriteLine("Device Information is null" );
                return false;
            }
            deviceSelector = $"System.Devices.Aep.DeviceAddress:=\"{devInfo.Properties["System.Devices.Aep.DeviceAddress"]}\"";
            DeviceInformationCollection pairedDeviceCollection = await DeviceInformation.FindAllAsync(deviceSelector, null, DeviceInformationKind.Device);
            Debug.WriteLine("Paired devices " + pairedDeviceCollection.Count + ": ");
            foreach (DeviceInformation dev in pairedDeviceCollection)
            {
                Debug.Write(dev);
            }
            return pairedDeviceCollection.Count > 0;
        }

        public ConnectedDevice[] GetConnectedDevices()
        {
            return connectedDevices.ToArray();
        }

        public static string DeviceToRemoteHost(ConnectedDevice device)
        {
            return $"${device.WfdDevice.GetConnectionEndpointPairs()[0].RemoteHostName}".Replace("$", "");
        }

        private void OnConnectionStatusChanged(WiFiDirectDevice wfdDevice, object arg)
        {
            Debug.WriteLine($"Connection status changed: {wfdDevice.ConnectionStatus}" );
            foreach(ConnectedDevice dev in connectedDevices)
            {
                if(dev.WfdDevice.DeviceId == wfdDevice.DeviceId)
                {
                    _ = dev.DeviceInfo.Pairing.UnpairAsync();
                    connectedDevices.Remove(dev);
                    break;
                }
            }

        }
    }

    public static class Debug
    {
        private static string log = "";
        public static void WriteLine(object str)
        {
            log += str.ToString() + "\n";
        }

        public static void Write(object str)
        {
            log += str.ToString();
        }

        public static void WriteLineIf(bool b, object str)
        {
            if (b)
            {
                log += str.ToString() + "\n";
            }
        }

        public static string GetLog() {
            return log;
        }
    }
}
