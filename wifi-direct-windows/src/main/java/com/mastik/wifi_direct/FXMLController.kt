package com.mastik.wifi_direct

import com.mastik.wifi_direct.components.DeviceComponent
import com.mastik.wifi_direct.csharp.DiscoveredDevice
import com.mastik.wifi_direct.csharp.Watcher
import javafx.application.Platform
import javafx.fxml.FXML
import javafx.fxml.Initializable
import javafx.scene.control.Button
import javafx.scene.control.Label
import javafx.scene.control.ScrollPane
import javafx.scene.layout.HBox
import javafx.scene.layout.VBox
import java.net.URL
import java.util.ResourceBundle


class FXMLController : Initializable {

    @FXML
    private var label: Label? = null

    @FXML
    private var devices: VBox? = null

    override fun initialize(url: URL, rb: ResourceBundle?) {
        val javaVersion = System.getProperty("java.version")
        val javafxVersion = System.getProperty("javafx.version")
        label!!.text = "Hello, JavaFX $javafxVersion\nRunning on Java $javaVersion."
    }

    fun addDevice(device: DiscoveredDevice){
        println(device.getDisplayName())

        val deviceComponent = DeviceComponent(device)

        Platform.runLater {
            devices!!.children.add(deviceComponent)
        }
    }

    fun removeDevice(device: DiscoveredDevice){
        devices!!.children.removeIf(){
            it is DeviceComponent && it.device.getId() == device.getId()
        }
    }
}