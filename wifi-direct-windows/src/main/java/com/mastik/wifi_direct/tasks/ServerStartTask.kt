package com.mastik.wifi_direct.tasks

import java.io.FileDescriptor
import java.net.BindException
import java.net.ServerSocket
import java.util.function.Consumer
import java.util.function.Supplier

class ServerStartTask(
    private val defaultPort: Int,
    ) : Communicator, Runnable {

    companion object {
        val TAG: String = ServerStartTask::class.simpleName!!

        const val MAX_PORT_OFFSET = 10
    }

    private var communicator: SocketCommunicator = SocketCommunicator()

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

            server.close()

            communicator.readLoop(client)
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }

    override fun getMessageSender(): Consumer<String> {
        return communicator.getMessageSender()
    }

    override fun setOnNewMessageListener(onNewMessage: Consumer<String>) {
        communicator.setOnNewMessageListener(onNewMessage)
    }

    override fun getFileSender(): Consumer<FileDescriptor> {
        return communicator.getFileSender()
    }

    override fun setOnNewFileListener(onNewFile: Supplier<FileDescriptor>) {
        communicator.setOnNewFileListener(onNewFile)
    }
}