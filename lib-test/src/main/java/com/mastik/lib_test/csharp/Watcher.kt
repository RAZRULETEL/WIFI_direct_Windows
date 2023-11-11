package com.mastik.lib_test.csharp

import com.javonet.sdk.internal.InvocationContext
import com.mastik.lib_test.csharp.Advertiser.Companion.INFO_REQUEST_PERIOD
import java.util.TimerTask
import java.util.function.Consumer

class Watcher {
    companion object {
        const val START_DISCOVERING = "StartWatching"
        const val STOP_DISCOVERING = "StopWatching"
        const val GET_DISCOVERED_DEVICES = "GetDiscoveredDevices"
        const val CONNECT_DEVICE = "ConnectDevice"
    }

    val instance = Config.createCSObject("Watcher")
    val advertiser: Advertiser

    private val discoveredDevices = mutableSetOf<String>()
    private var newDiscoveredDeviceListener: Consumer<DiscoveredDevice>? = null

    protected var isDiscovering: Boolean = false
        private set

    init {
        Thread.sleep(100)
        advertiser = Advertiser(instance.getInstanceField("advertiser"))
        advertiser.internalTimer.scheduleAtFixedRate(object : TimerTask() {
            override fun run() {
                val devices =
                    instance.invokeInstanceMethod(GET_DISCOVERED_DEVICES)
                if(devices.getSize().execute().value as Int >= 0)
                    for(csDevice in devices.execute().iterator()){
                        val device = DiscoveredDevice(csDevice)
                        if(!discoveredDevices.contains(device.getId())){
                            discoveredDevices.add(device.getId())
                            newDiscoveredDeviceListener?.accept(device)
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
        return res
    }

    fun stopDiscovering(): Boolean {
        if (!isDiscovering) return true
        val res = instance.invokeInstanceMethod(STOP_DISCOVERING).execute().value as Boolean
        isDiscovering = false
        advertiser.isAdvertising = false
        return res
    }

    fun getDiscoveredDevices(): Any{
        return instance.invokeInstanceMethod("devices")
//        return discoveredDevices;
    }

    fun connectDevice(discoveredDevice: DiscoveredDevice){
        if(!isDiscovering) return
        instance.invokeInstanceMethod(CONNECT_DEVICE, discoveredDevice.context).execute()
    }

    fun setOnNewDiscoveredDevice(onNewDeviceListener: Consumer<DiscoveredDevice>) {
        newDiscoveredDeviceListener = onNewDeviceListener
    }
}