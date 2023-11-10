package com.mastik.lib_test;

import com.mastik.lib_test.csharp.Advertiser;
import com.mastik.lib_test.csharp.Logger;

public class MyClass {

    public static void main(String[] args) throws InterruptedException {
        var advertiser = new Advertiser();

        System.out.println("Start advertisement: " + advertiser.startAdvertisement());

        advertiser.setOnNewPairedDevice(device -> System.out.println("New Device: "+device));
        var log = Logger.INSTANCE;

        Thread.sleep(3_000);

        System.out.println(Logger.INSTANCE.getLog());
        System.out.println("Stop advertisement: " + advertiser.stopAdvertisement());
    }
}