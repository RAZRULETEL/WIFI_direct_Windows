package com.mastik.wifi_direct.tasks

import com.mastik.wifi_direct.transfer.AbstractCommunicatorTask
import com.mastik.wifi_direct.transfer.Communicator
import com.mastik.wifi_direct.transfer.FileDescriptorTransferInfo
import java.io.FileDescriptor
import java.net.BindException
import java.net.ServerSocket
import java.util.function.Consumer
import java.util.function.Function
import java.util.function.Supplier

class ServerStartTask(
    private val defaultPort: Int,
    ) : AbstractCommunicatorTask() {

    companion object {
        val TAG: String = ServerStartTask::class.simpleName!!

        const val MAX_PORT_OFFSET = 10
    }

    override fun run() {
        var server: ServerSocket? = null
        var portOffset = 0
        try {
            while (server == null) {
                if (portOffset >= MAX_PORT_OFFSET) {
                    println("Start socket listener error, port overflow")
                    return
                }
                try {
                    server = ServerSocket(defaultPort + portOffset++)
                } catch (_: BindException) {}
            }
        } catch (e: IllegalArgumentException) {
            println("Start socket listener error, invalid port: $defaultPort")
            return
        } catch (e: Exception) {
            println("Start socket listener unexpected error")
            return
        }



        try {
            val client = server.accept()

            println("Received client connection: $client")

            server.close()

            communicator.readLoop(client)
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }
}