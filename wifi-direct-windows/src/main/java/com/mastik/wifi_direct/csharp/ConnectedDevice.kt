package com.mastik.wifi_direct.csharp

import com.javonet.sdk.internal.InvocationContext
import com.mastik.wifi_direct.csharp.Advertiser.Companion.DEVICE_TO_HOST
import com.mastik.wifi_direct.csharp.Advertiser.Companion.STATIC_TYPE

class ConnectedDevice(val context: InvocationContext) {

    fun getRemoteAddress(): String{
        return STATIC_TYPE.invokeStaticMethod(DEVICE_TO_HOST, context).execute().value as String
    }

    fun getDisplayName(): String{
        return context.getInstanceField("DisplayName").execute().value as String
    }

    fun getId(): String{
        return Companion.getId(context);
    }

    companion object{
        fun getId(context: InvocationContext): String{
            return context.getInstanceField("DeviceInfo").getInstanceField("Id").execute().value as String
        }
    }
}