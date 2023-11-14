package com.mastik.wifi_direct.components

import com.mastik.wifi_direct.csharp.DiscoveredDevice
import com.mastik.wifi_direct.csharp.Watcher
import com.mastik.wifi_direct.enums.ConnectionStatus
import javafx.application.Platform
import javafx.scene.control.Button
import javafx.scene.control.Label
import javafx.scene.layout.HBox
import javafx.scene.layout.VBox

class DeviceComponent(val device: DiscoveredDevice): HBox() {

    private val status = Label("Available")
    private val connectButton = Button("▶")

    private var onStatusChange: ((ConnectionStatus) -> Unit)? = null

    init {
        val deviceInfo = VBox()

        deviceInfo.children.add(Label(device.getDisplayName()))

        deviceInfo.children.add(status)

        this.children.add(deviceInfo)
        this.children.add(connectButton)

        connectButton.setOnAction {
            Watcher.connectDevice(device)
            status.text = "Connecting..."
            connectButton.onAction = null
        }
    }

    fun changeStatus(status: ConnectionStatus) {
        Platform.runLater{
            this.status.text = status.textRepresentation
        }
        if(status == ConnectionStatus.CONNECTED){
            connectButton.setOnAction {
                Watcher.unpairDevice(device)
                connectButton.onAction = null
            }
        }
        onStatusChange?.invoke(status)
    }

    fun setOnStatusChange(onStatusChange: (ConnectionStatus) -> Unit) {
        this.onStatusChange = onStatusChange
    }
}