package com.mastik.wifi_direct.csharp

import com.javonet.sdk.internal.InvocationContext
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

        const val INFO_REQUEST_PERIOD: Long = 500L

        val STATIC_TYPE = Config.getStaticClass(Advertiser::class.java.simpleName)
    }

    constructor(): this(Config.createCSObject("Advertiser"))

    private var newDeviceListener: Consumer<ConnectedDevice>? = null
    private val connectedDevices = mutableSetOf<ConnectedDevice>()

    internal var isAdvertising: Boolean = false

    public val internalTimer = Timer()

    init {
        internalTimer.scheduleAtFixedRate(object : TimerTask() {
            override fun run() {
                val devices = instance.invokeInstanceMethod(GET_CONNECTED_DEVICES)
                for (device in devices.execute().iterator()){
                    var isFound = false
                    for(connectedDevice in connectedDevices){
                        if(connectedDevice.getId() == ConnectedDevice.getId(device)){
                            isFound = true
                            break
                        }
                    }

                    if(!isFound) {
                        val connectedDevice = ConnectedDevice(device)
                        connectedDevices.add(connectedDevice)
                        newDeviceListener?.accept(connectedDevice)
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

    fun setOnNewPairedDevice(onNewDeviceListener: Consumer<ConnectedDevice>) {
        newDeviceListener = onNewDeviceListener
    }
}