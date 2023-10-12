using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDKTemplate;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.UI.Popups;
using System.Threading;
using System.Diagnostics;

namespace SDKTemplate
{
    public class Advertiser
    {
        private WiFiDirectAdvertisementPublisher publisher;
        private WiFiDirectConnectionListener listener;

        public Advertiser()
        {

        }

        public bool startAdvertisement()
        {
            publisher = new WiFiDirectAdvertisementPublisher();
            listener = new WiFiDirectConnectionListener();

            var discoverability = WiFiDirectAdvertisementListenStateDiscoverability.Normal;
            publisher.Advertisement.ListenStateDiscoverability = discoverability;

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

            publisher.StatusChanged += OnStatusChanged;

            publisher.Start();

            return publisher.Status == WiFiDirectAdvertisementPublisherStatus.Started;
        }

        public void stopAdvertisement()
        {
            publisher.Stop();

            publisher.StatusChanged -= OnStatusChanged;

            listener.ConnectionRequested -= OnConnectionRequested;
        }

        private void OnStatusChanged(WiFiDirectAdvertisementPublisher sender, WiFiDirectAdvertisementPublisherStatusChangedEventArgs statusEventArgs)
        {
            Debug.WriteLine(statusEventArgs.Status);
        }

            private async void OnConnectionRequested(WiFiDirectConnectionListener sender, WiFiDirectConnectionRequestedEventArgs connectionEventArgs)
        {
            WiFiDirectConnectionRequest connectionRequest = connectionEventArgs.GetConnectionRequest();
            bool success = //await Dispatcher.RunTaskAsync(async () =>{
                /*return*/ await HandleConnectionRequestAsync(connectionRequest);
            //});

            if (!success)
            {
                // Decline the connection request
                connectionRequest.Dispose();
            }
        }

        private async Task<bool> HandleConnectionRequestAsync(WiFiDirectConnectionRequest connectionRequest)
        {
            string deviceName = connectionRequest.DeviceInformation.Name;

            bool isPaired = (connectionRequest.DeviceInformation.Pairing?.IsPaired == true) ||
                            (await IsAepPairedAsync(connectionRequest.DeviceInformation.Id));

            // Show the prompt only in case of WiFiDirect reconnection or Legacy client connection.
            if (isPaired || publisher.Advertisement.LegacySettings.IsEnabled)
            {
                var messageDialog = new MessageDialog($"Connection request received from {deviceName}", "Connection Request");

                // Add two commands, distinguished by their tag.
                // The default command is "Decline", and if the user cancels, we treat it as "Decline".
                messageDialog.Commands.Add(new UICommand("Accept", null, true));
                messageDialog.Commands.Add(new UICommand("Decline", null, null));
                messageDialog.DefaultCommandIndex = 1;
                messageDialog.CancelCommandIndex = 1;

                // Show the message dialog
                var commandChosen = await messageDialog.ShowAsync();

                if (commandChosen.Id == null)
                {
                    return false;
                }
            }

            Debug.WriteLine($"Connecting to {deviceName}...", NotifyType.StatusMessage);

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

            // Register for the ConnectionStatusChanged event handler
            //wfdDevice.ConnectionStatusChanged += OnConnectionStatusChanged;

            var listenerSocket = new StreamSocketListener();

            // Save this (listenerSocket, wfdDevice) pair so we can hook it up when the socket connection is made.
            //_pendingConnections[listenerSocket] = wfdDevice;

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

            Debug.WriteLine($"Devices connected on L2, listening on IP Address: {EndpointPairs[0].LocalHostName}" +
                                $" Port: {Globals.strServerPort}", NotifyType.StatusMessage);
            return true;
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
            return pairedDeviceCollection.Count > 0;
        }


        public async Task<bool> RequestPairDeviceAsync(DeviceInformationPairing pairing)
        {
            WiFiDirectConnectionParameters connectionParams = new WiFiDirectConnectionParameters();

            /*short? groupOwnerIntent = 0;//Utils.GetSelectedItemTag<short?>(cmbGOIntent);
            if (groupOwnerIntent.HasValue)
            {
                connectionParams.GroupOwnerIntent = groupOwnerIntent.Value;
            }*/

            DevicePairingKinds devicePairingKinds = DevicePairingKinds.None;

            // If specific configuration methods were added, then use them.
/*            if (supportedConfigMethods.Count > 0)
            {
                foreach (var configMethod in supportedConfigMethods)
                {
                    connectionParams.PreferenceOrderedConfigurationMethods.Add(configMethod);
                    devicePairingKinds |= WiFiDirectConnectionParameters.GetDevicePairingKinds(configMethod);
                }
            }
            else
            {*/
                // If specific configuration methods were not added, then we'll use these pairing kinds.
                devicePairingKinds = DevicePairingKinds.ConfirmOnly | DevicePairingKinds.DisplayPin | DevicePairingKinds.ProvidePin;
           /* }*/

            Debug.WriteLine(devicePairingKinds.ToString());

            connectionParams.PreferredPairingProcedure = WiFiDirectPairingProcedure.GroupOwnerNegotiation;//Utils.GetSelectedItemTag<WiFiDirectPairingProcedure>(cmbPreferredPairingProcedure);
            DeviceInformationCustomPairing customPairing = pairing.Custom;
            customPairing.PairingRequested += OnPairingRequested;

            DevicePairingResult result = await customPairing.PairAsync(devicePairingKinds, DevicePairingProtectionLevel.Default, connectionParams);
            if (result.Status != DevicePairingResultStatus.Paired)
            {
                Debug.WriteLine($"PairAsync failed, Status: {result.Status}", NotifyType.ErrorMessage);
                return false;
            }
            return true;
        }

        private void OnPairingRequested(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            HandlePairing(args);
        }

        private static void HandlePairing(DevicePairingRequestedEventArgs args)
        {
            using (Deferral deferral = args.GetDeferral())
            {
                Debug.WriteLine("Handle pairing");
                switch (args.PairingKind)
                {
                    case DevicePairingKinds.DisplayPin:
                        //await ShowPinToUserAsync(dispatcher, args.Pin);
                        Debug.WriteLine(args.Pin);
                        args.Accept();
                        break;

                    case DevicePairingKinds.ConfirmOnly:
                        Debug.WriteLine("Accept pin");
                        args.Accept();
                        break;

                    case DevicePairingKinds.ProvidePin:
                        {
                            Debug.WriteLine("Provide pin");
                            /*string pin = await GetPinFromUserAsync(dispatcher);
                            if (!String.IsNullOrEmpty(pin))
                            {
                                args.Accept(pin);
                            }*/
                        }
                        break;
                }
            }
        }

        private async void OnSocketConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            //var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            //Dispatcher(async () => {
                Debug.WriteLine("Connecting to remote side on L4 layer...", NotifyType.StatusMessage);
                StreamSocket serverSocket = args.Socket;

                // Look up the WiFiDirectDevice associated with this StreamSocketListener.
                WiFiDirectDevice wfdDevice;
                /*if (!_pendingConnections.TryRemove(sender, out wfdDevice))
                {
                    rootPage.NotifyUser("Unexpected connection ignored.", NotifyType.ErrorMessage);
                    serverSocket.Dispose();
                    return;
                }*/

                SocketReaderWriter socketRW = new SocketReaderWriter(serverSocket, null);

                // The first message sent is the name of the connection.
                string message = await socketRW.ReadMessageAsync();

            // Add this connection to the list of active connections.
            //ConnectedDevices.Add(new ConnectedDevice(message ?? "(unnamed)", wfdDevice, socketRW));
            Debug.WriteLine(message ?? "(unnamed)");

                while (message != null)
                {
                    message = await socketRW.ReadMessageAsync();
                    Debug.WriteLine($"{message}");
                }
            //});
        }
    }
}
