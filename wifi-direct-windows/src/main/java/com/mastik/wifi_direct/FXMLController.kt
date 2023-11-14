package com.mastik.wifi_direct

import com.mastik.wifi_direct.components.DeviceComponent
import com.mastik.wifi_direct.csharp.Config
import com.mastik.wifi_direct.csharp.DiscoveredDevice
import com.mastik.wifi_direct.csharp.Watcher
import javafx.application.Platform
import javafx.fxml.FXML
import javafx.fxml.Initializable
import javafx.scene.control.Button
import javafx.scene.control.Label
import javafx.scene.control.MenuItem
import javafx.scene.control.ScrollPane
import javafx.scene.layout.HBox
import javafx.scene.layout.VBox
import java.net.URL
import java.util.ResourceBundle


class FXMLController : Initializable {

    @FXML
    private var log: Label? = null

    @FXML
    private var getLog: Button? = null

    @FXML
    private var devices: VBox? = null

    @FXML
    private var restartWatcher: MenuItem? = null

    override fun initialize(url: URL, rb: ResourceBundle?) {
        getLog!!.setOnAction {
            log!!.text = Config.getStaticClass("Debug").invokeStaticMethod("GetLog").execute().value as String
        }

        devices!!.children.clear()

        restartWatcher!!.setOnAction {
            Watcher.stopDiscovering()
            Watcher.startDiscovering()
        }
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

    fun getDevices(): List<DeviceComponent>{
        return devices!!.children.map{
            it as DeviceComponent
        }
    }
}