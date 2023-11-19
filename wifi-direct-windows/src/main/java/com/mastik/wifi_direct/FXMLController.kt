package com.mastik.wifi_direct

import com.mastik.wifi_direct.components.DeviceComponent
import com.mastik.wifi_direct.csharp.Config
import com.mastik.wifi_direct.csharp.DiscoveredDevice
import com.mastik.wifi_direct.csharp.Watcher
import com.mastik.wifidirect.tasks.TaskExecutors
import javafx.application.Platform
import javafx.fxml.FXML
import javafx.fxml.Initializable
import javafx.scene.control.Alert
import javafx.scene.control.Button
import javafx.scene.control.Label
import javafx.scene.control.MenuItem
import javafx.scene.control.TextField
import javafx.scene.layout.VBox
import javafx.stage.FileChooser
import javafx.stage.Stage
import java.io.FileDescriptor
import java.io.FileInputStream
import java.io.FileOutputStream
import java.net.URL
import java.util.ResourceBundle
import java.util.function.Consumer


class FXMLController : Initializable {

    @FXML
    private var log: Label? = null

    @FXML
    private var sendMessage: Button? = null

    @FXML
    private var sendFile: Button? = null

    @FXML
    private var message: TextField? = null

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

    fun sendNotification(message: String){
        Platform.runLater(){
            val a = Alert(Alert.AlertType.INFORMATION)
            a.contentText = message
            a.show()
        }
    }

    fun setOnMessageSend(messageSender: Consumer<String>){
        sendMessage!!.setOnAction {
            messageSender.accept(message!!.text)
        }
    }

    fun setOnFileSend(fileSender: Consumer<FileDescriptor>, stage: Stage){
        sendFile!!.setOnAction {
            Platform.runLater(){
                val fileChooser = FileChooser()
                fileChooser.title = "Open Resource File"

                fileChooser.extensionFilters.addAll(
                    FileChooser.ExtensionFilter("Text Files", "*.txt"),
                    FileChooser.ExtensionFilter("Image Files", "*.png", "*.jpg", "*.gif"),
                    FileChooser.ExtensionFilter("Audio Files", "*.wav", "*.mp3", "*.aac"),
                    FileChooser.ExtensionFilter("All Files", "*")
                )

                val selectedFile = fileChooser.showOpenDialog(stage)

                println(selectedFile)

                TaskExecutors.getCachedPool().execute {
                    fileSender.accept(FileInputStream(selectedFile).fd)
                }
            }
        }
    }
}