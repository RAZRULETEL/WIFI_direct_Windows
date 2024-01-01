package com.mastik.wifi_direct.csharp

import com.javonet.sdk.internal.InvocationContext
import com.mastik.wifi_direct.csharp.Advertiser.Companion.DEVICE_TO_HOST
import com.mastik.wifi_direct.csharp.Advertiser.Companion.STATIC_TYPE

class ConnectedDevice(val context: InvocationContext): PhysicalDevice {

    fun getRemoteAddress(): String{
        return STATIC_TYPE.invokeStaticMethod(DEVICE_TO_HOST, context).execute().value as String
    }

    override fun getDisplayName(): String{
        return context.getInstanceField("DisplayName").execute().value as String
    }

    override fun getId(): String{
        return Companion.getId(context)
    }

    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false

        other as ConnectedDevice

        if (getId() != other.getId() || hashCode() != other.hashCode()) return false

        return true
    }

    override fun hashCode(): Int {
        return getId().hashCode() + getDisplayName().hashCode()
    }

    override fun toString(): String {
        return "ConnectedDevice(name=${getDisplayName()}, id=${getId()})"
    }

    companion object{
        fun getId(context: InvocationContext): String{
            return context.getInstanceField("DeviceInfo").getInstanceField("Id").execute().value as String
        }
    }
}