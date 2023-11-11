package com.mastik.lib_test;

import com.mastik.lib_test.csharp.Logger;
import com.mastik.lib_test.csharp.Watcher;

public class MyClass {

    public static void main(String[] args) throws InterruptedException {
        var watcher = new Watcher();

        System.out.println("Start advertisement: " + watcher.startDiscovering());

        watcher.getAdvertiser().setOnNewPairedDevice(device -> System.out.println("New Device: "+device));
        watcher.setOnNewDiscoveredDevice(device -> System.out.println("Find device: " + device.getDisplayName()));
        var log = Logger.INSTANCE;

        for(;;)
        Thread.sleep(3_000);

//        System.out.println(Logger.INSTANCE.getLog());
//        System.out.println("Stop advertisement: " + watcher.stopDiscovering());
    }
}