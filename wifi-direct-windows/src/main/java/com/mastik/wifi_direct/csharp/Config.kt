package com.mastik.wifi_direct.csharp

import com.javonet.sdk.internal.InvocationContext
import com.javonet.sdk.internal.RuntimeContext
import com.javonet.sdk.java.Javonet

object Config {
    private val calledRuntime: RuntimeContext = Javonet.inMemory().clr()
    private const val libraryPath = "./WiFiDirect.dll"
    private const val NAMESPACE = "WiFiDirectApi"
    init {
        calledRuntime.loadLibrary(libraryPath)
    }

    fun createCSObject(className: String): InvocationContext {
        return calledRuntime.getType("$NAMESPACE.$className").createInstance().execute()
    }

    fun getStaticClass(className: String): InvocationContext {
        return calledRuntime.getType("$NAMESPACE.$className").execute()
    }
}