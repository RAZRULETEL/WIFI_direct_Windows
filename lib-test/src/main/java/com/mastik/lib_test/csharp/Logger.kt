package com.mastik.lib_test.csharp

object Logger {
    private val logger = Config.getStaticClass("Debug")

    fun getLog(): String {
        return logger.invokeStaticMethod("GetLog").execute().value as String
    }
}