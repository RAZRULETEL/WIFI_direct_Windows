package com.mastik.lib_test.tasks

import java.io.IOException
import java.net.InetSocketAddress
import java.net.Socket
import java.net.SocketTimeoutException
import java.util.function.Consumer

class ConnectTask(
    private val host: String,
    private val defaultPort: Int,
    private val connectDelay: Long = 1_000L
): Communicator, Runnable {
    companion object{
        val TAG: String = ConnectTask::class.simpleName!!

        private const val CONNECT_TIMEOUT: Int = 3_000
    }

    private var communicator: SocketCommunicator = SocketCommunicator()

    override fun run() {
        Thread.sleep(connectDelay)

        val client = Socket()
        var portOffset = 0

        while (!client.isConnected) {
            if(portOffset >= ServerStartTask.MAX_PORT_OFFSET){
                println("Start socket listener error, port overflow")
                return
            }
            try {
                client.connect(
                    InetSocketAddress(host, defaultPort + portOffset++),
                    CONNECT_TIMEOUT
                )
            } catch (_: SocketTimeoutException) {
            } catch (_: IOException) {
            } catch (e: IllegalArgumentException) {
                println("Start socket listener error, invalid port or host")
                return
            }
        }



        try {
            communicator.readLoop(client)
            if(!client.isConnected) client.close()
        } catch (e: Exception){
            e.printStackTrace()
        }
    }

    override fun getMessageSender(): Consumer<String> {
        return communicator.getMessageSender()
    }

    override fun setOnNewMessageListener(onNewMessage: Consumer<String>) {
        communicator.setOnNewMessageListener(onNewMessage)
    }
}