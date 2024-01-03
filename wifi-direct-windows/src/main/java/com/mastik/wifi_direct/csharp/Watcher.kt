package com.mastik.wifi_direct.csharp

import com.mastik.wifi_direct.csharp.Advertiser.Companion.INFO_REQUEST_PERIOD
import javafx.collections.FXCollections
import javafx.collections.ObservableSet
import trikita.log.Log
import java.util.TimerTask

object Watcher {
    const val START_DISCOVERING = "StartWatching"
    const val STOP_DISCOVERING = "StopWatching"
    const val GET_DISCOVERED_DEVICES = "GetDiscoveredDevices"
    const val CONNECT_DEVICE = "ConnectDevice"

    val instance = Config.createCSObject("Watcher")
    val advertiser: Advertiser

    private val discoveredDevices = FXCollections.observableSet<DiscoveredDevice>()

    var isDiscovering: Boolean = false
        private set

    init {
        Thread.sleep(100)
        advertiser = Advertiser(instance.getInstanceField("advertiser"))
        advertiser.internalTimer.scheduleAtFixedRate(object : TimerTask() {
            override fun run() {
                val devices = instance.invokeInstanceMethod(GET_DISCOVERED_DEVICES)
                val discoveredIds = discoveredDevices.map { e -> e.getId() }
                val csDiscoveredIds = mutableListOf<String>()
                for (csDevice in devices.execute().iterator()) {
                    csDiscoveredIds.add(DiscoveredDevice.getId(csDevice))
                    if(!discoveredIds.contains(DiscoveredDevice.getId(csDevice))) {
                        val device = DiscoveredDevice(csDevice)
                        Log.d("New discovered device: $device")
                        discoveredDevices.add(device)
                    }
                }
                for(deviceId in discoveredIds){
                    if(!csDiscoveredIds.contains(deviceId)){
                        Log.d("Removed discovered device $deviceId")
                        discoveredDevices.removeIf { e -> e.getId() == deviceId }
                    }
                }
            }
        }, 100, INFO_REQUEST_PERIOD)
    }

    fun startDiscovering(): Boolean {
        if (isDiscovering) return true
        val res = instance.invokeInstanceMethod(START_DISCOVERING).execute().value as Boolean
        isDiscovering = res
        advertiser.isAdvertising = res
        Log.i("Start watcher: $res")
        return res
    }

    fun stopDiscovering(): Boolean {
        if (!isDiscovering) return true
        val res = instance.invokeInstanceMethod(STOP_DISCOVERING).execute().value as Boolean
        isDiscovering = false
        advertiser.isAdvertising = false
        Log.i("Stop watcher: $res")
        return res
    }

    fun connectDevice(discoveredDevice: DiscoveredDevice) {
        if (!isDiscovering) return
        instance.invokeInstanceMethod(CONNECT_DEVICE, discoveredDevice.context).execute()
    }

    fun unpairDevice(discoveredDevice: DiscoveredDevice) {
        discoveredDevice.context.getInstanceField("DeviceInfo").getInstanceField("Pairing")
            .invokeInstanceMethod("UnpairAsync").execute()
    }

    fun getDiscoveredDevices(): ObservableSet<DiscoveredDevice> {
        return discoveredDevices
    }
}