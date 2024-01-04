package com.mastik.wifi_direct

import com.mastik.wifi_direct.FXMLController.Companion.DEFAULT_FILE_FILTERS
import com.mastik.wifi_direct.csharp.Watcher
import com.mastik.wifi_direct.enums.ConnectionStatus
import com.mastik.wifi_direct.tasks.SocketConnectionManager
import com.mastik.wifi_direct.tasks.TaskExecutors
import com.mastik.wifi_direct.transfer.FileDescriptorTransferInfo
import javafx.application.Application
import javafx.application.Platform
import javafx.collections.SetChangeListener
import javafx.event.EventHandler
import javafx.fxml.FXMLLoader
import javafx.scene.Scene
import javafx.scene.text.Font
import javafx.stage.FileChooser
import javafx.stage.Stage
import trikita.log.Log
import java.io.File
import java.io.FileOutputStream
import java.util.concurrent.Exchanger
import java.util.concurrent.TimeUnit
import kotlin.system.exitProcess


class Main: Application() {
    companion object{
        const val DEFAULT_PORT = 50001

        @JvmStatic
        fun main(args: Array<String>) {
            Runtime.getRuntime()
                .addShutdownHook(Thread({
                    Log.i("Shutting down...")
                    Watcher.stopDiscovering()
                    Watcher.advertiser.stopAdvertisement()
                }, "Shutdown-thread"))

            launch(Main::class.java, *args)
        }
    }

    private lateinit var controller: FXMLController

    @Throws(Exception::class)
    override fun start(stage: Stage) {
        val root = FXMLLoader(javaClass.classLoader.getResource("scene.fxml"))
        val scene = Scene(root.load())
        scene.stylesheets.add(javaClass.classLoader.getResource("styles.css").toExternalForm())
        stage.title = "JavaFX and Gradle"
        stage.scene = scene
        stage.show()

        stage.onCloseRequest = EventHandler {
            Platform.exit()
            exitProcess(0)
        }

        controller = root.getController()
        controller.setStage(stage)
        Watcher.getDiscoveredDevices().addListener(SetChangeListener {
            if(it.wasAdded())
                controller.addOrUpdateDevice(it.elementAdded)
            if(it.wasRemoved())
                controller.removeDevice(it.elementRemoved)
        })

        Watcher.advertiser.getConnectedDevices().addListener(SetChangeListener {
            if(it.wasAdded()) {
                controller.addOrUpdateDevice(it.elementAdded).changeStatus(ConnectionStatus.CONNECTED)
                SocketConnectionManager.addDevice(it.elementAdded)
            }
            if(it.wasRemoved()) {
                controller.addOrUpdateDevice(it.elementAdded).changeStatus(ConnectionStatus.DISCONNECTED)
                SocketConnectionManager.removeDevice(it.elementRemoved)
            }
        })

        initSocketCommunicators(controller, stage)

        TaskExecutors.getCachedPool().execute {
            Watcher.startDiscovering()
        }
    }

    private fun initSocketCommunicators(controller: FXMLController, stage: Stage) {
        SocketConnectionManager.setOnNewMessageListener() {
            controller.sendNotification(it)
        }

        controller.setOnMessageSend(SocketConnectionManager.getMessageSender())

        SocketConnectionManager.setOnNewFileListener() {fileName ->
            val exchanger = Exchanger<File>()

            Platform.runLater(){
                val fileChooser = FileChooser()
                fileChooser.title = "Save Resource File"
                fileChooser.initialFileName = fileName

                fileChooser.extensionFilters.addAll(DEFAULT_FILE_FILTERS)

                val selectedFile = fileChooser.showSaveDialog(stage)

                exchanger.exchange(selectedFile, 100, TimeUnit.MILLISECONDS)
            }

            val file = exchanger.exchange(null)

            file?.let {
                val transferInfo = FileDescriptorTransferInfo(FileOutputStream(file).fd, file.name)
                controller.addFileReceiveProgressBar(transferInfo)
                return@setOnNewFileListener transferInfo
            }

            return@setOnNewFileListener null
        }
    }
}
