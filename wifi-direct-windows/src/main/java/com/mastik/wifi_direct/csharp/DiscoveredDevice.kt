package com.mastik.wifi_direct.csharp

import com.javonet.sdk.internal.InvocationContext

class DiscoveredDevice(val context: InvocationContext) {

    fun getDisplayName(): String{
        return context.getInstanceField("DisplayName").execute().value as String
    }

    fun getId(): String{
        return context.getInstanceField("DeviceInfo").getInstanceField("Id").execute().value as String
    }
}