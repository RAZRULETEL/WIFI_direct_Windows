package com.mastik.wifi_direct

import com.mastik.wifi_direct.csharp.Watcher
import com.mastik.wifi_direct.enums.ConnectionStatus
import com.mastik.wifi_direct.tasks.ServerStartTask
import com.mastik.wifidirect.tasks.TaskExecutors
import javafx.application.Application
import javafx.application.Platform
import javafx.event.EventHandler
import javafx.fxml.FXMLLoader
import javafx.scene.Scene
import javafx.stage.DirectoryChooser
import javafx.stage.Stage
import java.io.FileOutputStream
import java.nio.file.Path
import java.util.concurrent.Exchanger
import java.util.concurrent.TimeUnit
import kotlin.system.exitProcess


class Main: Application() {
    companion object{
        private const val DEFAULT_PORT = 50001

        @JvmStatic
        fun main(args: Array<String>) {
            Watcher.startDiscovering()
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
        Watcher.setOnNewDiscoveredDevice() {
            controller.addDevice(it)
        }

        Watcher.advertiser.setOnNewPairedDevice() {connectedDevice ->
            controller.getDevices().find { it.device.getId() == connectedDevice.getId() }
                ?.changeStatus(ConnectionStatus.CONNECTED)
        }

        initSocketCommunicators(controller)
    }

    private fun initSocketCommunicators(controller: FXMLController) {
        val startServerTask = ServerStartTask(DEFAULT_PORT)
        TaskExecutors.getFixedPool().execute(startServerTask)

        startServerTask.setOnNewMessageListener() {
            controller.sendNotification(it)
        }

        controller.setOnMessageSend(startServerTask.getMessageSender())

        startServerTask.setOnNewFileListener() {
            val exchanger = Exchanger<Path>()

            Platform.runLater(){
                val directoryChooser = DirectoryChooser()
                val selectedDirectory = directoryChooser.showDialog(null)
                exchanger.exchange(selectedDirectory?.toPath()?.resolve("test.txt"), 100, TimeUnit.MILLISECONDS)
            }

            val file = exchanger.exchange(null)

            // server task close their file output stream that will close descriptor, so we don't need to worry about it
            return@setOnNewFileListener FileOutputStream(file.toFile()).fd
        }


    }
}
