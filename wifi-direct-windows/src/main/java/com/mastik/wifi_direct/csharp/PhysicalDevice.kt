package com.mastik.wifi_direct.csharp

import java.lang.IllegalStateException
import java.util.regex.Pattern

interface PhysicalDevice {

    fun getDisplayName(): String
    fun getId(): String

    fun getMACAddress(): String{
        val pattern = Pattern.compile("([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})")
        val matcher = pattern.matcher(getId())
        if(!matcher.find()) throw IllegalStateException("MAC address not found")
        return matcher.group()
    }

    fun physicalEquals(device: PhysicalDevice): Boolean{
        return getMACAddress() == device.getMACAddress()
    }
}