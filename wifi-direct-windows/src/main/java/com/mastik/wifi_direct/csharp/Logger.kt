package com.mastik.wifi_direct.csharp

object Logger {
    private val logger = Config.getStaticClass("Debug")

    fun getLog(): String {
        return logger.invokeStaticMethod("GetLog").execute().value as String
    }
}