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
using System.Diagnostics;
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

        private void btnFromId_Click(object sender, RoutedEventArgs e)
        {
            var discoveredDevice = (DiscoveredDevice)lvDiscoveredDevices.SelectedItem;

            test.ConnectDevice(discoveredDevice);
        }


        private void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            test.SendMessage(txtSendMessage.Text);
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
            Debug.WriteLine($"Unpair result: {result.Status}", NotifyType.StatusMessage);
        }
    }
}
