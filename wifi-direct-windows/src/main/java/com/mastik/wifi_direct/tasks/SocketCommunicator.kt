package com.mastik.wifi_direct.tasks

import com.mastik.wifi_direct.tasks.Communicator.Companion.MAGIC_FILE_BYTE
import com.mastik.wifi_direct.tasks.Communicator.Companion.MAGIC_STRING_BYTE
import java.io.DataInputStream
import java.io.FileDescriptor
import java.io.FileInputStream
import java.io.FileOutputStream
import java.io.IOException
import java.io.InputStreamReader
import java.io.OutputStream
import java.io.OutputStreamWriter
import java.net.Socket
import java.nio.CharBuffer
import java.nio.charset.Charset
import java.util.concurrent.locks.ReentrantLock
import java.util.function.Consumer
import java.util.function.Supplier

class SocketCommunicator() : Communicator {
    companion object {
        val TAG: String = SocketCommunicator::class.simpleName!!

        const val INT_SIZE = 4 // integer size in bytes
    }

    private var mainOutStream: OutputStream? = null
    private var outTextStream: OutputStreamWriter? = null

    private val writeLock = ReentrantLock(true)

    private val onMessageSend: Consumer<String> = Consumer<String> { message ->
        outTextStream?.let {
            println("Send message: $message")

            val len = message.length
            try {
                it.write(MAGIC_STRING_BYTE)
                for (i in 0 until Int.SIZE_BYTES) it.write(len shr (i * 8))

                it.write(message)
                it.flush()
            } catch (e: IOException) {
                println("Send message error")
            }
        }
    }

    private val onFileSend: Consumer<FileDescriptor> = Consumer<FileDescriptor> { file ->
//        TaskExecutors.getFixedPool().execute {
        println("Send file")
        mainOutStream?.let {
            val fileStream = DataInputStream(FileInputStream(file))
            writeLock.lock()
            try {
                it.write(MAGIC_FILE_BYTE)
                for (i in 0 until Int.SIZE_BYTES) it.write(Int.MIN_VALUE shr (i * 8))
                while (fileStream.available() > 0) {
                    val arr = ByteArray(1024)
                    fileStream.readFully(arr, 0, 1024)
                    it.write(arr)
                }


            } catch (_: Exception) {
                writeLock.unlock()
            }
        }
//        }
    }

    private var newMessageListener: Consumer<String>? = null
    private var newFileListener: Supplier<FileDescriptor>? = null

    @Throws(IOException::class)
    fun readLoop(socket: Socket) {
        mainOutStream = socket.getOutputStream()
        outTextStream = OutputStreamWriter(socket.getOutputStream(), Charset.forName("UTF-8"))

        val stream = InputStreamReader(socket.getInputStream())
        val buff = CharArray(INT_SIZE)
        var messageBuff = CharBuffer.allocate(1024)

        while (socket.isConnected) {
            stream.read(buff, 0, 1)
            val magic = buff[0].code
            stream.read(buff, 0, Int.SIZE_BYTES)
            var dataSize = 0
            for (i in 0 until Int.SIZE_BYTES) dataSize += buff[i].code shl (i * 8)
            println("Received $dataSize bytes")
            if (magic == MAGIC_STRING_BYTE) {
                if (messageBuff.capacity() < dataSize)
                    messageBuff = CharBuffer.allocate(dataSize)
                stream.read(messageBuff)

                val message = messageBuff.position(0).toString().substring(0, dataSize)
                newMessageListener?.accept(message)
                messageBuff.clear()
            }
            if (magic == MAGIC_FILE_BYTE) {
                newFileListener?.get()?.let {
                    val fileStream = FileOutputStream(it)
                    while (dataSize > 0) {
                        stream.read(messageBuff.array(), 0, dataSize.coerceAtMost(1024))
                        fileStream.write(messageBuff.toList().stream().map { e -> e.code.toByte() }
                            .toList().toByteArray(), 0, dataSize.coerceAtMost(1024))
                        dataSize -= 1024
                        messageBuff.clear()
                    }
                    fileStream.close()
                }
            }
        }
    }

    override fun getMessageSender(): Consumer<String> {
        return onMessageSend
    }

    override fun setOnNewMessageListener(onNewMessage: Consumer<String>) {
        newMessageListener = onNewMessage
    }

    override fun getFileSender(): Consumer<FileDescriptor> {
        return onFileSend
    }

    override fun setOnNewFileListener(onNewFile: Supplier<FileDescriptor>) {
        newFileListener = onNewFile
    }
}