package com.mastik.wifi_direct

import com.mastik.wifi_direct.csharp.Config
import com.mastik.wifi_direct.csharp.Watcher
import com.mastik.wifi_direct.enums.ConnectionStatus
import javafx.application.Application
import javafx.application.Platform
import javafx.event.EventHandler
import javafx.fxml.FXMLLoader
import javafx.scene.Scene
import javafx.stage.Stage
import javafx.stage.WindowEvent
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



    @Throws(Exception::class)
    override fun start(stage: Stage) {
        val root: FXMLLoader = FXMLLoader(javaClass.classLoader.getResource("scene.fxml"))
        val scene = Scene(root.load())
        scene.stylesheets.add(javaClass.classLoader.getResource("styles.css").toExternalForm())
        stage.title = "JavaFX and Gradle"
        stage.scene = scene
        stage.show()

        stage.onCloseRequest = EventHandler<WindowEvent?> {
            Watcher.stopDiscovering()
            Platform.exit()
            exitProcess(0)
        }

        val controller = root.getController<FXMLController>()
        Watcher.setOnNewDiscoveredDevice() {
            controller.addDevice(it)
        }

        Watcher.advertiser.setOnNewPairedDevice() {connectedDevice ->
            controller.getDevices().find { it.device.getId() == connectedDevice.getId() }
                ?.changeStatus(ConnectionStatus.CONNECTED)
        }
    }
}
