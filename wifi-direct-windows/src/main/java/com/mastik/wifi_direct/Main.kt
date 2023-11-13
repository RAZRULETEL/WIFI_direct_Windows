package com.mastik.wifi_direct

import com.mastik.wifi_direct.csharp.DiscoveredDevice
import com.mastik.wifi_direct.csharp.Logger
import com.mastik.wifi_direct.csharp.Watcher
import com.mastik.wifi_direct.tasks.ConnectTask
import com.mastik.wifi_direct.tasks.ServerStartTask
import com.mastik.wifidirect.tasks.TaskExecutors.Companion.getFixedPool
import javafx.application.Application
import javafx.fxml.FXMLLoader
import javafx.scene.Scene
import javafx.stage.Stage

class Main: Application() {
    companion object{
        private const val DEFAULT_PORT = 50001


        @JvmStatic
        fun main(args: Array<String>) {
            launch(Main::class.java, *args)

            val watcher = Watcher()
            println("Start advertisement: " + watcher.startDiscovering())
            watcher.advertiser.setOnNewPairedDevice { device: String ->
                println("New Device: $device")
                val serverTask = ServerStartTask(Main.DEFAULT_PORT)
                serverTask.setOnNewMessageListener { x: String? ->
                    println(
                        x
                    )
                }
                getFixedPool().execute(serverTask)
                val connectTask =
                    ConnectTask(device, Main.DEFAULT_PORT, 1000)
                connectTask.setOnNewMessageListener { x: String? ->
                    println(
                        x
                    )
                }
                getFixedPool().execute(connectTask)
            }
            watcher.setOnNewDiscoveredDevice { device: DiscoveredDevice ->
                println("Find device: " + device.getDisplayName())
                watcher.connectDevice(device)
            }
            val log = Logger
            Main
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
    }
}
