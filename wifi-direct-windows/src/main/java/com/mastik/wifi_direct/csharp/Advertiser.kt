package com.mastik.wifi_direct.csharp

import com.javonet.sdk.internal.InvocationContext
import javafx.collections.FXCollections
import javafx.collections.ObservableSet
import java.net.InetSocketAddress
import java.net.SocketAddress
import java.util.Timer
import java.util.TimerTask
import java.util.function.Consumer

class Advertiser internal constructor(private val instance: InvocationContext) {
    companion object {
        const val START_ADVERTISEMENT = "StartAdvertisement"
        const val STOP_ADVERTISEMENT = "StopAdvertisement"
        const val GET_CONNECTED_DEVICES = "GetConnectedDevices"

        const val DEVICE_TO_HOST = "DeviceToRemoteHost"

        const val INFO_REQUEST_PERIOD: Long = 300L

        val STATIC_TYPE = Config.getStaticClass(Advertiser::class.java.simpleName)
    }

    constructor(): this(Config.createCSObject("Advertiser"))

    private val connectedDevices = FXCollections.observableSet<ConnectedDevice>()

    internal var isAdvertising: Boolean = false

    public val internalTimer = Timer()

    init {
        internalTimer.scheduleAtFixedRate(object : TimerTask() {
            override fun run() {
                val devices = instance.invokeInstanceMethod(GET_CONNECTED_DEVICES)
                val discoveredIds = connectedDevices.map { e -> e.getId() }
                val csDiscoveredIds = mutableListOf<String>()
                for (csDevice in devices.execute().iterator()) {
                    csDiscoveredIds.add(ConnectedDevice.getId(csDevice))
                    if(!discoveredIds.contains(ConnectedDevice.getId(csDevice))) {
                        val device = ConnectedDevice(csDevice)
                        println("New connected device: ${device.getDisplayName()}")
                        connectedDevices.add(device)
                    }
                }
                for(deviceId in discoveredIds){
                    if(!csDiscoveredIds.contains(deviceId)){
                        println("Removed connected device $deviceId")
                        connectedDevices.removeIf { e -> e.getId() == deviceId }
                    }
                }
            }
        }, 100, INFO_REQUEST_PERIOD);
    }

    fun startAdvertisement(): Boolean {
        if(isAdvertising) return true
        val res = instance.invokeInstanceMethod(START_ADVERTISEMENT).execute().value as Boolean
        isAdvertising = res
        return res
    }

    fun stopAdvertisement(): Boolean {
        if(!isAdvertising) return true
        val res = instance.invokeInstanceMethod(STOP_ADVERTISEMENT).execute().value as Boolean
        isAdvertising = false
        return res
    }

    fun getConnectedDevices(): ObservableSet<ConnectedDevice> {
        return connectedDevices
    }
}