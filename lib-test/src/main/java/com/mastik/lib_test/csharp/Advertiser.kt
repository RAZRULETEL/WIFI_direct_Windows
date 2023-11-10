package com.mastik.lib_test.csharp

import java.net.InetSocketAddress
import java.net.SocketAddress
import java.util.Timer
import java.util.TimerTask
import java.util.function.Consumer

class Advertiser {
    companion object {
        const val START_ADVERTISEMENT = "StartAdvertisement"
        const val STOP_ADVERTISEMENT = "StopAdvertisement"
        const val GET_CONNECTED_DEVICES = "GetConnectedDevices"
    }

    private val instance = Config.createCSObject("Advertiser")
    private var newDeviceListener: Consumer<SocketAddress>? = null
    private val connectedDevices = mutableSetOf<SocketAddress>()

    init {
        Timer().scheduleAtFixedRate(object : TimerTask() {
            override fun run() {
                val devices = instance.invokeInstanceMethod(GET_CONNECTED_DEVICES).execute().value as String?
                devices?.let {
                    it.split(":").forEach { device ->
                        InetSocketAddress(device.replace("\$", ""), 0).let {
                            if (!connectedDevices.contains(it)) {
                                connectedDevices.add(it)
                                newDeviceListener?.accept(it)
                            }
                        }
                    }
                }
            }
        }, 100, 500);
    }

    fun startAdvertisement(): Boolean {
        return instance.invokeInstanceMethod(START_ADVERTISEMENT).execute().value as Boolean
    }

    fun stopAdvertisement(): Boolean {
        return instance.invokeInstanceMethod(STOP_ADVERTISEMENT).execute().value as Boolean
    }

    fun setOnNewPairedDevice(onNewDeviceListener: Consumer<SocketAddress>) {
        newDeviceListener = onNewDeviceListener
    }
}