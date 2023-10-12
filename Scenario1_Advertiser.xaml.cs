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

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using WiFiDirectApi;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SDKTemplate
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Scenario1_Advertiser : Page
    {
        private MainPage rootPage = MainPage.Current;
        Advertiser test = new Advertiser();

        public Scenario1_Advertiser()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (btnStopAdvertisement.IsEnabled)
            {
                test.StopAdvertisement();

                btnStartAdvertisement.IsEnabled = true;
                btnStopAdvertisement.IsEnabled = false;
            }
        }

        private void btnStartAdvertisement_Click(object sender, RoutedEventArgs e)
        {
            if (test.StartAdvertisement())
            {
                btnStartAdvertisement.IsEnabled = false;
                btnStopAdvertisement.IsEnabled = true;
            }
        }

        private async void btnAddIe_Click(object sender, RoutedEventArgs e)
        {
            test.SendMessage(txtInformationElement.Text);
        }

        private void btnStopAdvertisement_Click(object sender, RoutedEventArgs e)
        {
            if (test.StopAdvertisement())
            {
                btnStartAdvertisement.IsEnabled = true;
                btnStopAdvertisement.IsEnabled = false;
            }
        }
    }
}

