package com.mastik.wifi_direct

import com.mastik.wifi_direct.components.DeviceComponent
import com.mastik.wifi_direct.csharp.Config
import com.mastik.wifi_direct.csharp.DiscoveredDevice
import com.mastik.wifi_direct.csharp.PhysicalDevice
import com.mastik.wifi_direct.csharp.Watcher
import com.mastik.wifi_direct.tasks.TaskExecutors
import com.mastik.wifi_direct.transfer.FileDescriptorTransferInfo
import javafx.application.Platform
import javafx.event.ActionEvent
import javafx.event.EventHandler
import javafx.fxml.FXML
import javafx.fxml.Initializable
import javafx.geometry.Insets
import javafx.scene.control.Alert
import javafx.scene.control.Button
import javafx.scene.control.Label
import javafx.scene.control.MenuItem
import javafx.scene.control.ProgressBar
import javafx.scene.control.TextField
import javafx.scene.layout.HBox
import javafx.scene.layout.VBox
import javafx.stage.FileChooser
import javafx.stage.Stage
import trikita.log.Log
import java.io.FileInputStream
import java.net.URL
import java.util.ResourceBundle
import java.util.function.Consumer


class FXMLController : Initializable {
    companion object{
        val DEFAULT_FILE_FILTERS = listOf(FileChooser.ExtensionFilter("All Files", "*"),
            FileChooser.ExtensionFilter("Documents", "*.docx", "*.odt", "*.pdf", "*.rtf", "*.txt", "*.html", "*.xml"),
            FileChooser.ExtensionFilter("Image Files", "*.png", "*.jpg", "*.gif", "*.webp", "*.svg", "*.bmp"),
            FileChooser.ExtensionFilter("Audio Files", "*.wav", "*.mp3", "*.aac"),
            FileChooser.ExtensionFilter("Video Files", "*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv", "*.flv", "*.webm")
        )
    }

    @FXML
    private var log: Label? = null

    @FXML
    private var sendMessage: Button? = null

    @FXML
    private var message: TextField? = null

    @FXML
    private var getLog: Button? = null

    @FXML
    private var devices: VBox? = null

    @FXML
    private var restartWatcher: MenuItem? = null

    private lateinit var stage: Stage

    fun setStage(stage: Stage) {
        this.stage = stage
    }

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

    fun addDevice(device: PhysicalDevice): DeviceComponent{
        val deviceComponent = DeviceComponent(device, this)

        Platform.runLater {
            devices!!.children.add(deviceComponent)
        }

        return deviceComponent
    }

    fun addOrUpdateDevice(device: PhysicalDevice): DeviceComponent{
        return getDevices().find { e -> device.physicalEquals(e.device) }?.also { it.updateDevice(device) } ?: addDevice(device)
    }

    fun removeDevice(device: DiscoveredDevice){
        Platform.runLater {
            devices!!.children.removeIf() {
                it is DeviceComponent && it.device.getId() == device.getId()
            }
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

    fun setOnFileSend(fileSender: Consumer<FileDescriptorTransferInfo>): EventHandler<ActionEvent> {
        return EventHandler<ActionEvent> {
            Platform.runLater(){
                val fileChooser = FileChooser()
                fileChooser.title = "Open Resource File"

                fileChooser.extensionFilters.addAll(DEFAULT_FILE_FILTERS)

                val selectedFile = fileChooser.showOpenDialog(stage)

                Log.d("Sending file: $selectedFile")

                if(selectedFile != null)
                    TaskExecutors.getCachedPool().execute {
                        fileSender.accept(FileDescriptorTransferInfo(FileInputStream(selectedFile).fd, selectedFile.name))
                    }
            }
        }
    }

    fun addFileReceiveProgressBar(descriptorInfo: FileDescriptorTransferInfo){
        val hBox = HBox()

        val nameLabel = Label(descriptorInfo.name)
        val progressBar = ProgressBar(0.0)
        val etaLabel = Label("ETA: 00:00")
        val speedLabel = Label("Speed: 0 KB/s")

        nameLabel.minWidth = 100.0
        progressBar.minWidth = 100.0
        etaLabel.minWidth = 50.0
        speedLabel.minWidth = 80.0

        progressBar.padding = Insets(0.0, 5.0, 0.0, 5.0)
        speedLabel.padding = Insets(0.0, 0.0, 0.0, 5.0)

        hBox.children += nameLabel
        hBox.children += progressBar
        hBox.children += etaLabel
        hBox.children += speedLabel

        Platform.runLater {
            (log!!.parent as VBox).children.add(1, hBox)
        }

        descriptorInfo.addProgressListener{
            Platform.runLater {
                progressBar.progress = it.bytesProgress.toDouble() / it.bytesTotal
                etaLabel.text = "ETA: ${(it.ETA / 60).toInt()}:${(it.ETA % 60).toInt()}"
                speedLabel.text = "Speed: ${(it.currentSpeed / 1024).toInt()} KB/s"
            }
        }

        descriptorInfo.addTransferEndListener {
            Platform.runLater{
                (log!!.parent as VBox).children.remove(hBox)
            }
        }
    }
}