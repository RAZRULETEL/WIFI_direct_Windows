package com.mastik.wifi_direct.tasks

import com.mastik.wifi_direct.Main
import com.mastik.wifi_direct.csharp.ConnectedDevice
import com.mastik.wifi_direct.csharp.PhysicalDevice
import com.mastik.wifi_direct.transfer.Communicator
import com.mastik.wifi_direct.transfer.FileDescriptorTransferInfo
import trikita.log.Log
import java.util.function.Consumer
import java.util.function.Function

object SocketConnectionManager: Communicator {
    private var connectManagers: MutableMap<String, MultiConnectTask> = mutableMapOf()
    private var serverTask: ServerStartTask? = null

    init {
        restartServerTask()
    }

    private var onNewFile: Function<String, FileDescriptorTransferInfo?>? = null
    private var onNewMessage: Consumer<String>? = null

    fun addDevice(newDevice: ConnectedDevice){
        if(!ServerStartTask.isServerRunning())
            restartServerTask()

        connectManagers[newDevice.getMACAddress()] = MultiConnectTask(newDevice.getRemoteAddress(), Main.DEFAULT_PORT)
    }

    fun removeDevice(device: PhysicalDevice){
        connectManagers.remove(device.getMACAddress())?.destroy()
    }

    override fun getFileSender(): Consumer<FileDescriptorTransferInfo> = Consumer {transferInfo ->
        Log.d("Sending file $transferInfo to ${connectManagers.keys}")
        connectManagers.values.forEach { e -> TaskExecutors.getCachedPool().execute { e.getFileSender().accept(transferInfo) } }
    }

    override fun getMessageSender(): Consumer<String>  = Consumer { message ->
        connectManagers.values.forEach { e -> e.getMessageSender().accept(message) }
    }

    override fun setOnNewFileListener(onNewFile: Function<String, FileDescriptorTransferInfo?>) {
        this.onNewFile = onNewFile
    }

    override fun setOnNewMessageListener(onNewMessage: Consumer<String>) {
        this.onNewMessage = onNewMessage
    }

    private fun restartServerTask(){
        serverTask = ServerStartTask(Main.DEFAULT_PORT)
        TaskExecutors.getCachedPool().execute(serverTask)

        serverTask!!.setOnNewClientListener {
            Log.d("Received client $it")
        }
        serverTask!!.setOnNewMessageListener{
            onNewMessage?.accept(it)
        }
        serverTask!!.setOnNewFileListener{
            return@setOnNewFileListener onNewFile?.apply(it)
        }
    }
}
