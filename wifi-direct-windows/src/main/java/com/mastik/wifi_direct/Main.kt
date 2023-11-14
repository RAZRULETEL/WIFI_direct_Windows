package com.mastik.wifi_direct

import com.mastik.wifi_direct.csharp.Watcher
import javafx.application.Application
import javafx.application.Platform
import javafx.event.EventHandler
import javafx.fxml.FXMLLoader
import javafx.scene.Scene
import javafx.stage.Stage
import javafx.stage.WindowEvent

class Main: Application() {
    companion object{
        private const val DEFAULT_PORT = 50001

        @JvmStatic
        fun main(args: Array<String>) {
            println("Start watcher: ${Watcher.startDiscovering()}")
            launch(Main::class.java, *args)

            while (true) {
                Thread.sleep(3000)
            }
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

        stage.onCloseRequest = EventHandler<WindowEvent?> { Platform.exit() }

        val controller = root.getController<FXMLController>()
        Watcher.setOnNewDiscoveredDevice() {
            controller.addDevice(it)
        }
    }
}
