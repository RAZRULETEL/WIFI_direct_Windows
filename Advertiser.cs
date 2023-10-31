using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using SDKTemplate;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace WiFiDirectApi
{

    public class Advertiser
    {

        private WiFiDirectAdvertisementPublisher publisher;
        private WiFiDirectConnectionListener listener;
        private ObservableCollection<SocketReaderWriter> connectedDevices = new ObservableCollection<SocketReaderWriter>();
        
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
            publisher.Stop();

            if (publisher.Status != WiFiDirectAdvertisementPublisherStatus.Aborted)
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
            bool success = await System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
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

            Debug.WriteLine($"Connecting to {deviceName}..." );

            // Show the prompt only in case of WiFiDirect reconnection or Legacy client connection.
            if (isPaired || publisher.Advertisement.LegacySettings.IsEnabled)
            {
                Debug.WriteLineIf(isPaired, "Reconnect!");
                Debug.WriteLineIf(publisher.Advertisement.LegacySettings.IsEnabled, "Legacy connection!");
            }

                // Pair device if not already paired and not using legacy settings
            if (!isPaired && !publisher.Advertisement.LegacySettings.IsEnabled)
            {
                Debug.WriteLine(connectionRequest.DeviceInformation);
                if (!await RequestPairDeviceAsync(connectionRequest.DeviceInformation.Pairing, false))
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
                Debug.WriteLine($"Exception in FromIdAsync: {ex}" );
                return false;
            }
            wfdDevice.ConnectionStatusChanged += OnConnectionStatusChanged;

            if (!await StartSocketListener(wfdDevice))
            {
                return false;
            }
            // Windows do not have api to get know who is group owner and there is no way to guarantee group owner role
            // So we starts server and tries to connect
            RequestSocketTransfer(wfdDevice);

            return true;
        }

        static public async Task<bool> RequestPairDeviceAsync(DeviceInformationPairing pairing, bool isConnectRequest = true)
        {
            Debug.WriteLine("Trying pair device async");
            WiFiDirectConnectionParameters connectionParams = new WiFiDirectConnectionParameters();
            connectionParams.GroupOwnerIntent = (short)(15 - (isConnectRequest ? 1 : 0));


            DevicePairingKinds devicePairingKinds = DevicePairingKinds.ConfirmOnly | DevicePairingKinds.ConfirmPinMatch
                | DevicePairingKinds.DisplayPin;


            connectionParams.PreferredPairingProcedure = WiFiDirectPairingProcedure.GroupOwnerNegotiation;
            DeviceInformationCustomPairing customPairing = pairing.Custom;
            customPairing.PairingRequested += OnPairingRequested;


            DevicePairingResult result = await customPairing.PairAsync(devicePairingKinds, DevicePairingProtectionLevel.None, connectionParams);
            if (result.Status != DevicePairingResultStatus.Paired)
            {
                Debug.WriteLine($"PairAsync failed, Status: {result.Status}" );
                return false;
            }
            return true;
        }

        static private void OnPairingRequested(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            Debug.WriteLine("On pairing request " + args.PairingKind);
            Utils.HandlePairing(System.Windows.Threading.Dispatcher.CurrentDispatcher, args);
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

        private void OnSocketConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var task = System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
            async () =>
            {
                Debug.WriteLine("Accepting remote side on L4 layer..." );
                StreamSocket serverSocket = args.Socket;

                SocketReaderWriter socketRW = new SocketReaderWriter(serverSocket);

                await socketRW.WriteMessageAsync("Hello from Moon ");

                connectedDevices.Add(socketRW);

                // The first message sent is the name of the connection.
                string message = await socketRW.ReadMessageAsync();

                // Add this connection to the list of active connections.
                Debug.WriteLine(message ?? "(unnamed)" );

                while (message != null)
                {
                    message = await socketRW.ReadMessageAsync();
                    Debug.WriteLine($"{message}" );
                }
                connectedDevices.Remove(socketRW);
                socketRW.Dispose();
            });
        }

        private void OnConnectionStatusChanged(WiFiDirectDevice sender, object arg)
        {
            Debug.WriteLine($"Connection status changed: {sender.ConnectionStatus}" );

        }

        public void RequestSocketTransfer(WiFiDirectDevice wfdDevice)
        {
            var task = System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
            async () =>
            {
                IReadOnlyList<EndpointPair> endpointPairs = wfdDevice.GetConnectionEndpointPairs();
                HostName remoteHostName = endpointPairs[0].RemoteHostName;

                // Wait for server to start listening on a socket
                await Task.Delay(2_000); // Prevent situation when two Windows clients connect each other at same time

                if (connectedDevices.Count == 0)
                {
                    Debug.WriteLine($"Connecting device on L2 layer, connecting to IP Address: {remoteHostName} Port: {Globals.strServerPort}");


                    // Connect to Advertiser on L4 layer
                    StreamSocket clientSocket = new StreamSocket();
                    try
                    {
                        await clientSocket.ConnectAsync(remoteHostName, Globals.strServerPort);
                        Debug.WriteLine("Connected with remote side on L4 layer" );
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Connect operation threw an exception: {ex.Message}" );
                        return;
                    }

                    SocketReaderWriter socketRW = new SocketReaderWriter(clientSocket);

                    await socketRW.WriteMessageAsync("Hello from Moon ");

                    string message = await socketRW.ReadMessageAsync();

                    Debug.WriteLine(message ?? "(unnamed)");

                    while (message != null)
                    {
                        message = await socketRW.ReadMessageAsync();
                        Debug.WriteLine($"{message}");
                    }
                    connectedDevices.Remove(socketRW);
                    socketRW.Dispose();
                }
            });
        }

        public async Task<bool> StartSocketListener(WiFiDirectDevice wfdDevice)
        {
            var listenerSocket = new StreamSocketListener();

            var EndpointPairs = wfdDevice.GetConnectionEndpointPairs();

            Debug.WriteLine($"Starting socket listener on L2, listening on IP Address: {EndpointPairs[0].LocalHostName}" +
                    $" Port: {Globals.strServerPort}" );


            listenerSocket.ConnectionReceived += OnSocketConnectionReceived;
            try
            {
                await listenerSocket.BindEndpointAsync(EndpointPairs[0].LocalHostName, Globals.strServerPort);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Connect operation threw an exception: {ex.Message}" );
                return false;
            }
            return true;
        }

        public async void SendMessage(String msg)
        {
            if (connectedDevices.Count > 0)
            {
                foreach (var sock in connectedDevices)
                {
                    await sock.WriteMessageAsync(msg);
                }
            }
            else{
                Debug.WriteLine("Socket is null!");
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
