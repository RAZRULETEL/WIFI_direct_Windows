package com.mastik.wifi_direct.csharp

import com.javonet.sdk.internal.InvocationContext

class DiscoveredDevice(val context: InvocationContext) {

    fun getDisplayName(): String{
        return context.getInstanceField("DisplayName").execute().value as String
    }

    fun getId(): String{
        return Companion.getId(context)
    }

    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false

        other as DiscoveredDevice

        if (getId() != other.getId() || hashCode() != other.hashCode()) return false

        return true
    }

    override fun hashCode(): Int {
        return getId().hashCode() + getDisplayName().hashCode()
    }

    companion object{
        fun getId(context: InvocationContext): String{
            return context.getInstanceField("DeviceInfo").getInstanceField("Id").execute().value as String
        }
    }
}