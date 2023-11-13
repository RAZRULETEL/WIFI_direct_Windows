package com.mastik.lib_test;

import com.mastik.lib_test.csharp.Logger;
import com.mastik.lib_test.csharp.Watcher;
import com.mastik.lib_test.tasks.ConnectTask;
import com.mastik.lib_test.tasks.ServerStartTask;
import com.mastik.wifidirect.tasks.TaskExecutors;

public class MyClass {
    public static final int DEFAULT_PORT = 50_001;


    public static void main(String[] args) throws InterruptedException {
        var watcher = new Watcher();

        System.out.println("Start advertisement: " + watcher.startDiscovering());

        watcher.getAdvertiser().setOnNewPairedDevice(device -> {
            System.out.println("New Device: "+device);

            var serverTask = new ServerStartTask(DEFAULT_PORT);
            serverTask.setOnNewMessageListener(System.out::println);
            TaskExecutors.Companion.getFixedPool().execute(serverTask);

            var connectTask = new ConnectTask(device, DEFAULT_PORT, 1_000);
            connectTask.setOnNewMessageListener(System.out::println);
            TaskExecutors.Companion.getFixedPool().execute(connectTask);
        });
        watcher.setOnNewDiscoveredDevice(device -> {
            System.out.println("Find device: " + device.getDisplayName());
            watcher.connectDevice(device);
        });
        var log = Logger.INSTANCE;

        for(;;)
        Thread.sleep(3_000);

//        System.out.println(Logger.INSTANCE.getLog());
//        System.out.println("Stop advertisement: " + watcher.stopDiscovering());
    }
}