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
import java.net.SocketException
import java.nio.CharBuffer
import java.nio.charset.Charset
import java.util.concurrent.locks.ReentrantLock
import java.util.function.Consumer
import java.util.function.Supplier
import kotlin.math.min

class SocketCommunicator() : Communicator {
    companion object {
        val TAG: String = SocketCommunicator::class.simpleName!!

        const val DEFAULT_BUFFER_SIZE: Int = 32_768
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
                for (i in 0 until Int.SIZE_BYTES) it.write(len shr (i * Byte.SIZE_BITS))

                it.write(message)
                it.flush()
            } catch (_: IOException) {
                println("Send message error")
            } finally {
                writeLock.unlock()
            }
        }
    }

    private val onFileSend: Consumer<FileDescriptor> = Consumer<FileDescriptor> { file ->
        println("Send file")
        mainOutStream?.let {
            val fileStream = DataInputStream(FileInputStream(file))
            writeLock.lock()
            try {
                it.write(MAGIC_FILE_BYTE)
                it.flush()
                for (i in 0 until Int.SIZE_BYTES) mainOutStream!!.write(fileStream.available() shr (i * Byte.SIZE_BITS))
                it.flush()
                val arr = ByteArray(DEFAULT_BUFFER_SIZE)
                while (fileStream.available() > 0) {
                    println("Available: ${fileStream.available()}")
                    val toRead = min(fileStream.available(), arr.size)
                    fileStream.readFully(arr, 0, toRead)
                    it.write(arr, 0, toRead)
                }
                it.flush()
            } catch (_: Exception) {
            } finally {
                writeLock.unlock()
            }
        }
    }

    private var newMessageListener: Consumer<String>? = null
    private var newFileListener: Supplier<FileDescriptor>? = null

    @Throws(IOException::class)
    fun readLoop(socket: Socket) {
        mainOutStream = socket.getOutputStream()
        outTextStream = OutputStreamWriter(socket.getOutputStream(), Charset.forName("UTF-8"))

        val rawStream = socket.getInputStream()
        val stream = InputStreamReader(socket.getInputStream())

        var messageBuff = CharBuffer.allocate(1024)
        val byteArray = ByteArray(Int.SIZE_BYTES)

        while (socket.isConnected) {
            rawStream.read(byteArray, 0, 1)
            val magic = byteArray[0].toInt() and 0xFF
            rawStream.read(byteArray, 0, Int.SIZE_BYTES)
            var dataSize = 0
            for (i in 0 until Int.SIZE_BYTES) dataSize += (byteArray[i].toInt() and 0xFF) shl (i * Byte.SIZE_BITS)
            println("Received $dataSize bytes")
            if (magic == MAGIC_STRING_BYTE) {
                if (messageBuff.capacity() < dataSize)
                    messageBuff = CharBuffer.allocate(dataSize)
                stream.read(messageBuff)

                val message = messageBuff.position(0).toString().substring(0, dataSize)
                newMessageListener?.accept(message)
                messageBuff.clear()
                continue
            }
            if (magic == MAGIC_FILE_BYTE) {
                try {
                    newFileListener?.get()?.let {
                        val fileStream = FileOutputStream(it)
                        val buffer = ByteArray(DEFAULT_BUFFER_SIZE)

                        var total: Long = 0
                        val start = System.currentTimeMillis()
                        var i = 0
                        while (dataSize > 0) {
                            val toRead = min(dataSize, buffer.size)
                            rawStream.readNBytes(buffer, 0, toRead)// Some slower than read, but data is not damaged
                            dataSize -= toRead
                            fileStream.write(buffer, 0, toRead)

                            total += toRead
                            if (i % 400 == 0) {
                                val cost = System.currentTimeMillis() - start
                                System.out.printf(
                                    "Readed %,d bytes, speed: %,f MB/s, left: %,d bytes %n",
                                    total,
                                    total.toDouble() / cost / 1000,
                                    dataSize
                                )
                            }
                            i++
                        }
                        fileStream.close()
                        println("Successfully readed file")

                    }
                } catch (e: Exception) {
                    e.printStackTrace()
                }
                continue
            }
            println("Unknown magic number $magic")
            if(magic == 0) throw SocketException("Unknown magic number")
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