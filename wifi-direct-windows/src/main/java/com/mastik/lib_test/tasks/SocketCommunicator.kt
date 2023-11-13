package com.mastik.lib_test.tasks

import java.io.IOException
import java.io.InputStreamReader
import java.io.OutputStreamWriter
import java.net.Socket
import java.nio.CharBuffer
import java.nio.charset.Charset
import java.util.function.Consumer

class SocketCommunicator (): Communicator {
    companion object{
        val TAG: String = SocketCommunicator::class.simpleName!!

        const val INT_SIZE = 4 // integer size in bytes
    }

    private var outTextStream: OutputStreamWriter? = null

    private val onMessageSend: Consumer<String> = Consumer<String>{ message ->
        outTextStream?.let {
            println("Send message: $message")

            val len = message.length
            try {
                for (i in 0 until INT_SIZE) it.write(len shr (i * 8))

                it.write(message)
                it.flush()
            } catch (e: IOException) {
                println("Send message error")
            }
        }
    }

    private var newMessageListener: Consumer<String>? = null

    @Throws(IOException::class)
    fun readLoop(socket: Socket) {
        outTextStream = OutputStreamWriter(socket.getOutputStream(), Charset.forName("UTF-8"))

        val stream = InputStreamReader(socket.getInputStream())
        val buff = CharArray(INT_SIZE)
        var messageBuff = CharBuffer.allocate(1024)

        while (socket.isConnected) {
            stream.read(buff, 0, INT_SIZE)
            var dataSize = 0
            for (i in 0 until INT_SIZE) dataSize += buff[i].code shl (i * 8)

            if(messageBuff.capacity() < dataSize)
                messageBuff = CharBuffer.allocate(dataSize)
            stream.read(messageBuff)

            val message = messageBuff.position(0).toString().substring(0, dataSize)
//            println("Received %d bytes: %s", dataSize, message)
            newMessageListener?.accept(message)
            messageBuff.clear()
        }
    }

    override fun getMessageSender(): Consumer<String> {
        return onMessageSend
    }

    override fun setOnNewMessageListener(onNewMessage: Consumer<String>) {
        newMessageListener = onNewMessage
    }
}