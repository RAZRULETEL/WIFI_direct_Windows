package com.mastik.wifi_direct

import com.mastik.wifi_direct.FXMLController.Companion.DEFAULT_FILE_FILTERS
import com.mastik.wifi_direct.csharp.Watcher
import com.mastik.wifi_direct.enums.ConnectionStatus
import com.mastik.wifi_direct.tasks.ServerStartTask
import com.mastik.wifi_direct.tasks.TaskExecutors
import com.mastik.wifi_direct.transfer.FileDescriptorTransferInfo
import javafx.application.Application
import javafx.application.Platform
import javafx.collections.SetChangeListener
import javafx.event.EventHandler
import javafx.fxml.FXMLLoader
import javafx.scene.Scene
import javafx.stage.FileChooser
import javafx.stage.Stage
import java.io.File
import java.io.FileOutputStream
import java.util.concurrent.Exchanger
import java.util.concurrent.TimeUnit
import kotlin.system.exitProcess


class Main: Application() {
    companion object{
        private const val DEFAULT_PORT = 50001

        @JvmStatic
        fun main(args: Array<String>) {
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
            Watcher.stopDiscovering()
            Platform.exit()
            exitProcess(0)
        }

        controller = root.getController()
        Watcher.getDiscoveredDevices().addListener(SetChangeListener {
            if(it.wasAdded())
                controller.addDevice(it.elementAdded)
            if(it.wasRemoved())
                controller.removeDevice(it.elementRemoved)
        })

        Watcher.advertiser.getConnectedDevices().addListener(SetChangeListener {
            if(it.wasAdded())
                controller.getDevices().find { e -> e.device.getId() == it.elementAdded.getId() }
                    ?.changeStatus(ConnectionStatus.CONNECTED)
            if(it.wasRemoved())
                controller.getDevices().find { e -> e.device.getId() == it.elementRemoved.getId() }
                    ?.changeStatus(ConnectionStatus.DISCONNECTED)
        })

        initSocketCommunicators(controller, stage)

        TaskExecutors.getCachedPool().execute {
            Watcher.startDiscovering()
        }
    }

    private fun initSocketCommunicators(controller: FXMLController, stage: Stage) {
        val startServerTask = ServerStartTask(DEFAULT_PORT)
        TaskExecutors.getCachedPool().execute(startServerTask)

        startServerTask.setOnNewMessageListener() {
            controller.sendNotification(it)
        }

        controller.setOnMessageSend(startServerTask.getMessageSender())

        startServerTask.setOnNewFileListener() {fileName ->
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

        controller.setOnFileSend(startServerTask.getFileSender(), stage)
    }
}
