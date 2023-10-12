using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKTemplate;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Devices.WiFiDirect;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace WiFiDirectApi
{

    public class MiniAdvertiser
    {
        private WiFiDirectAdvertisementPublisher publisher;
        private WiFiDirectConnectionListener listener;
        private MainPage rootPage;
        private SocketReaderWriter socketRW;
        public MiniAdvertiser(MainPage page)
        {
            rootPage = page;
        }
        public bool startAdvertisement()
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
                Debug.WriteLine($"Error preparing Advertisement: {ex}", NotifyType.ErrorMessage);
                return false;
            }

            publisher.Start();

            return publisher.Status == WiFiDirectAdvertisementPublisherStatus.Started;
        }

        public bool stopAdvertisement()
        {
            publisher.Stop();

            if (publisher.Status == WiFiDirectAdvertisementPublisherStatus.Stopped)
            {

                publisher.StatusChanged -= OnStatusChanged;

                listener.ConnectionRequested -= OnConnectionRequested;
                return true;
            }
            return false;
        }

        private void OnStatusChanged(WiFiDirectAdvertisementPublisher sender, WiFiDirectAdvertisementPublisherStatusChangedEventArgs statusEventArgs)
        {
            Debug.WriteLine(statusEventArgs.Status);
        }

        private async void OnConnectionRequested(WiFiDirectConnectionListener sender, WiFiDirectConnectionRequestedEventArgs connectionEventArgs)
        {
            Debug.WriteLine("Connection requested!!!");
            WiFiDirectConnectionRequest connectionRequest = connectionEventArgs.GetConnectionRequest();
            bool success = await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunTaskAsync(
                async () =>
                {
                    return await HandleConnectionRequestAsync(connectionRequest);
                });

            if (!success)
            {
                Debug.WriteLine("Pair request was declined !!!");
                connectionRequest.Dispose();
            }
        }

        private async Task<bool> HandleConnectionRequestAsync(WiFiDirectConnectionRequest connectionRequest)
        {
            string deviceName = connectionRequest.DeviceInformation.Name;

            bool isPaired = (connectionRequest.DeviceInformation.Pairing?.IsPaired == true) ||
                            (await IsAepPairedAsync(connectionRequest.DeviceInformation.Id));

            Debug.WriteLine($"Connecting to {deviceName}...", NotifyType.StatusMessage);

            // Show the prompt only in case of WiFiDirect reconnection or Legacy client connection.
            if (isPaired || publisher.Advertisement.LegacySettings.IsEnabled)
            {
                Debug.WriteLineIf(isPaired, "Reconnect!");
                Debug.WriteLineIf(publisher.Advertisement.LegacySettings.IsEnabled, "Legacy connection!");
            }

                // Pair device if not already paired and not using legacy settings
            if (!isPaired && !publisher.Advertisement.LegacySettings.IsEnabled)
            {
                if (!await RequestPairDeviceAsync(connectionRequest.DeviceInformation.Pairing))
                {
                    return false;
                }
            }

            WiFiDirectDevice wfdDevice = null;
            try
            {
                // IMPORTANT: FromIdAsync needs to be called from the UI thread
                wfdDevice = await WiFiDirectDevice.FromIdAsync(connectionRequest.DeviceInformation.Id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in FromIdAsync: {ex}", NotifyType.ErrorMessage);
                return false;
            }

            wfdDevice.ConnectionStatusChanged += OnConnectionStatusChanged;

            var listenerSocket = new StreamSocketListener();

            var EndpointPairs = wfdDevice.GetConnectionEndpointPairs();

            listenerSocket.ConnectionReceived += OnSocketConnectionReceived;
            try
            {
                await listenerSocket.BindEndpointAsync(EndpointPairs[0].LocalHostName, Globals.strServerPort);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Connect operation threw an exception: {ex.Message}", NotifyType.ErrorMessage);
                return false;
            }

            //RequestSocketTransfer(wfdDevice);

            Debug.WriteLine($"Devices connected on L2, listening on IP Address: {EndpointPairs[0].LocalHostName}" +
                                $" Port: {Globals.strServerPort}", NotifyType.StatusMessage);
            return true;
        }

        public async Task<bool> RequestPairDeviceAsync(DeviceInformationPairing pairing)
        {
            Debug.WriteLine("Trying pair device async");
            WiFiDirectConnectionParameters connectionParams = new WiFiDirectConnectionParameters();
            DevicePairingKinds devicePairingKinds = DevicePairingKinds.ConfirmOnly | DevicePairingKinds.ConfirmPinMatch
                | DevicePairingKinds.DisplayPin;

            connectionParams.PreferredPairingProcedure = WiFiDirectPairingProcedure.GroupOwnerNegotiation;
            DeviceInformationCustomPairing customPairing = pairing.Custom;
            customPairing.PairingRequested += OnPairingRequested;

            DevicePairingResult result = await customPairing.PairAsync(devicePairingKinds, DevicePairingProtectionLevel.None, connectionParams);
            if (result.Status != DevicePairingResultStatus.Paired)
            {
                rootPage.NotifyUser($"PairAsync failed, Status: {result.Status}", NotifyType.ErrorMessage);
                return false;
            }
            return true;
        }

        private void OnPairingRequested(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            Debug.WriteLine("On pairing request " + args.PairingKind);
            Utils.HandlePairing(Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher, args);
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
                Debug.WriteLine("DeviceInformation.CreateFromIdAsync threw an exception: " + ex.Message, NotifyType.ErrorMessage);
            }

            if (devInfo == null)
            {
                Debug.WriteLine("Device Information is null", NotifyType.ErrorMessage);
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

        private void OnSocketConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var task = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunTaskAsync(
            async () =>
            {
                rootPage.NotifyUser("Connecting to remote side on L4 layer...", NotifyType.StatusMessage);
                StreamSocket serverSocket = args.Socket;

                socketRW = new SocketReaderWriter(serverSocket, null);

                await socketRW.WriteMessageAsync("Hello from Moon ");

                // The first message sent is the name of the connection.
                string message = await socketRW.ReadMessageAsync();

                // Add this connection to the list of active connections.
                //ConnectedDevices.Add(new ConnectedDevice(message ?? "(unnamed)", wfdDevice, socketRW));
                rootPage.NotifyUser(message ?? "(unnamed)", NotifyType.StatusMessage);

                while (message != null)
                {
                    message = await socketRW.ReadMessageAsync();
                    rootPage.NotifyUser($"{message}", NotifyType.StatusMessage);
                }
            });
        }

        private void OnConnectionStatusChanged(WiFiDirectDevice sender, object arg)
        {
            Debug.WriteLine($"Connection status changed: {sender.ConnectionStatus}", NotifyType.StatusMessage);

        }

        private void RequestSocketTransfer(WiFiDirectDevice wfdDevice)
        {
            var task = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunTaskAsync(
            async () =>
            {
                IReadOnlyList<EndpointPair> endpointPairs = wfdDevice.GetConnectionEndpointPairs();
                HostName remoteHostName = endpointPairs[0].RemoteHostName;

                Debug.WriteLine($"Devices connected on L2 layer, connecting to IP Address: {remoteHostName} Port: {Globals.strServerPort}",
                    NotifyType.StatusMessage);

                // Wait for server to start listening on a socket
                await Task.Delay(2_000);

                // Connect to Advertiser on L4 layer
                StreamSocket clientSocket = new StreamSocket();
                try
                {
                    await clientSocket.ConnectAsync(remoteHostName, Globals.strServerPort);
                    Debug.WriteLine("Connected with remote side on L4 layer", NotifyType.StatusMessage);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Connect operation threw an exception: {ex.Message}", NotifyType.ErrorMessage);
                    return;
                }

                socketRW = new SocketReaderWriter(clientSocket, null);

                await socketRW.WriteMessageAsync("Hello from Moon ");

                string message = await socketRW.ReadMessageAsync();

                Debug.WriteLine(message ?? "(unnamed)");

                while (message != null)
                {
                    message = await socketRW.ReadMessageAsync();
                    Debug.WriteLine($"{message}");
                }

            });
        }

        public async void sendMessage(String msg)
        {
            if (socketRW != null)
            {
                await socketRW.WriteMessageAsync(msg);
            }
            else{
                Debug.WriteLine("Socket is null!");
            }
        }
    }
}
