//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using WiFiDirectApi;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SDKTemplate
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Scenario2_Connector : Page
    {
        private MainPage rootPage = MainPage.Current;


        Watcher test = new Watcher();

        public ObservableCollection<DiscoveredDevice> DiscoveredDevices = null;
        public ObservableCollection<DiscoveredDevice> ConnectedDevices = null;


        public Scenario2_Connector()
        {
            this.InitializeComponent();
            DiscoveredDevices = test.GetDiscoveredDevices();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (test.IsWatching())
            {
                test.StopWatching();
            }
        }

        private void btnWatcher_Click(object sender, RoutedEventArgs e)
        {
            if (!test.IsWatching())
            {
                if (test.StartWatching())
                {
                    btnWatcher.Content = "Stop Watcher";
                }
            }
            else
            {
                test.StopWatching();

                btnWatcher.Content = "Start Watcher";
            }
        }

        private void btnIe_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private async void btnFromId_Click(object sender, RoutedEventArgs e)
        {
            var discoveredDevice = (DiscoveredDevice)lvDiscoveredDevices.SelectedItem;

            if (discoveredDevice == null)
            {
                rootPage.NotifyUser("No device selected, please select one.", NotifyType.ErrorMessage);
                return;
            }

            rootPage.NotifyUser($"Connecting to {discoveredDevice.DeviceInfo.Name}...", NotifyType.StatusMessage);

            if (!discoveredDevice.DeviceInfo.Pairing.IsPaired)
            {
                if (!await connectionSettingsPanel.RequestPairDeviceAsync(discoveredDevice.DeviceInfo.Pairing))
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
                rootPage.NotifyUser("FromIdAsync was canceled by user", NotifyType.ErrorMessage);
                return;
            }

            // Register for the ConnectionStatusChanged event handler
            wfdDevice.ConnectionStatusChanged += OnConnectionStatusChanged;

            IReadOnlyList<EndpointPair> endpointPairs = wfdDevice.GetConnectionEndpointPairs();
            HostName remoteHostName = endpointPairs[0].RemoteHostName;

            rootPage.NotifyUser($"Devices connected on L2 layer, connecting to IP Address: {remoteHostName} Port: {Globals.strServerPort}",
                NotifyType.StatusMessage);

            // Wait for server to start listening on a socket
            await Task.Delay(2000);

            // Connect to Advertiser on L4 layer
            StreamSocket clientSocket = new StreamSocket();
            try
{
                await clientSocket.ConnectAsync(remoteHostName, Globals.strServerPort);
                rootPage.NotifyUser("Connected with remote side on L4 layer", NotifyType.StatusMessage);
            }
            catch (Exception ex)
            {
                rootPage.NotifyUser($"Connect operation threw an exception: {ex.Message}", NotifyType.ErrorMessage);
                return;
            }

            SocketReaderWriter socketRW = new SocketReaderWriter(clientSocket, rootPage);

            string sessionId = Path.GetRandomFileName();
            ConnectedDevice connectedDevice = new ConnectedDevice(sessionId, wfdDevice, socketRW);
            //ConnectedDevices.Add(connectedDevice);

            // The first message sent over the socket is the name of the connection.
            await socketRW.WriteMessageAsync(sessionId);

            while (await socketRW.ReadMessageAsync() != null)
            {
                // Keep reading messages
            }
        }

        private void OnConnectionStatusChanged(WiFiDirectDevice sender, object arg)
        {
            rootPage.NotifyUserFromBackground($"Connection status changed: {sender.ConnectionStatus}", NotifyType.StatusMessage);
        }

        private async void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            var connectedDevice = (ConnectedDevice)lvConnectedDevices.SelectedItem;
            await connectedDevice.SocketRW.WriteMessageAsync(txtSendMessage.Text);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            var connectedDevice = (ConnectedDevice)lvConnectedDevices.SelectedItem;
            //ConnectedDevices.Remove(connectedDevice);

            // Close socket and WiFiDirect object
            connectedDevice.Dispose();
        }

        private async void btnUnpair_Click(object sender, RoutedEventArgs e)
        {
            var discoveredDevice = (DiscoveredDevice)lvDiscoveredDevices.SelectedItem;

            DeviceUnpairingResult result = await discoveredDevice.DeviceInfo.Pairing.UnpairAsync();
            rootPage.NotifyUser($"Unpair result: {result.Status}", NotifyType.StatusMessage);
        }
    }
}
