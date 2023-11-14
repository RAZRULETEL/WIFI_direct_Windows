package com.mastik.wifi_direct.components

import com.mastik.wifi_direct.csharp.DiscoveredDevice
import com.mastik.wifi_direct.csharp.Watcher
import javafx.scene.control.Button
import javafx.scene.control.Label
import javafx.scene.layout.HBox
import javafx.scene.layout.VBox

class DeviceComponent(val device: DiscoveredDevice): HBox() {
    init {
        val deviceLayout = HBox()
        val deviceInfo = VBox()
        deviceInfo.children.add(Label(device.getDisplayName()))

        val status = Label("Available")
        deviceInfo.children.add(status)
        deviceLayout.children.add(deviceInfo)

        val connectButton = Button("▶️")
        deviceLayout.children.add(connectButton)

        connectButton.setOnAction {
            Watcher.connectDevice(device)
            status.text = "Connecting..."
            connectButton.onAction = null
        }
    }
}