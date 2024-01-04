package com.mastik.wifi_direct.components

import com.mastik.wifi_direct.FXMLController
import com.mastik.wifi_direct.Main.Companion.DEFAULT_PORT
import com.mastik.wifi_direct.csharp.ConnectedDevice
import com.mastik.wifi_direct.csharp.DiscoveredDevice
import com.mastik.wifi_direct.csharp.PhysicalDevice
import com.mastik.wifi_direct.csharp.Watcher
import com.mastik.wifi_direct.enums.ConnectionStatus
import com.mastik.wifi_direct.tasks.MultiConnectTask
import javafx.application.Platform
import javafx.scene.control.Button
import javafx.scene.control.Label
import javafx.scene.layout.HBox
import javafx.scene.layout.VBox

class DeviceComponent(val device: PhysicalDevice, private val controller: FXMLController) : HBox() {

    private val status =
        Label(if (device is ConnectedDevice) ConnectionStatus.CONNECTED.textRepresentation else ConnectionStatus.AVAILABLE.textRepresentation)
    private val connectButton = Button("▶").also {
        it.styleClass.add("connect-button")
    }
    private val sendButton = Button("🛜").also {
        it.isVisible = false
        it.styleClass.add("send-button")
    }

    private var onStatusChange: ((ConnectionStatus) -> Unit)? = null

    private var discoveredDevice: DiscoveredDevice? = null
    private var connectedDevice: ConnectedDevice? = null

    private var sender: MultiConnectTask? = null

    init {
        updateDevice(device)

        val deviceInfo = VBox()

        deviceInfo.children.add(Label(device.getDisplayName()))

        deviceInfo.children.add(status)

        this.children.add(deviceInfo)
        this.children.add(connectButton)
        this.children.add(sendButton)

        connectButton.setOnAction {
            discoveredDevice?.let { Watcher.connectDevice(it) }
            status.text = ConnectionStatus.CONNECTING.textRepresentation
            connectButton.onAction = null
        }
    }

    fun updateDevice(device: PhysicalDevice) {
        if (device is DiscoveredDevice) {
            discoveredDevice = device
        } else if (device is ConnectedDevice) {
            connectedDevice = device
            sendButton.isVisible = true

            connectedDevice?.let {
                if(sender == null)
                    sender = MultiConnectTask(it.getRemoteAddress(), DEFAULT_PORT)
            }

            sendButton.onAction = controller.setOnFileSend(sender!!.getFileSender())
        }
    }

    fun changeStatus(status: ConnectionStatus) {
        Platform.runLater {
            this.status.text = status.textRepresentation
        }
        if (status == ConnectionStatus.CONNECTED) {
            Platform.runLater {
                connectButton.text = "❌"
                connectButton.styleClass.add("disconnect")
            }
            connectButton.setOnAction {
                discoveredDevice?.let { Watcher.unpairDevice(it) }
                connectButton.onAction = null
            }
        }
        onStatusChange?.invoke(status)
    }

    fun setOnStatusChange(onStatusChange: (ConnectionStatus) -> Unit) {
        this.onStatusChange = onStatusChange
    }
}