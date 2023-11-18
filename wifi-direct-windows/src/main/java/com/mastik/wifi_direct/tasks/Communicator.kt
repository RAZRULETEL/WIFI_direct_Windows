package com.mastik.wifi_direct.tasks

import java.io.FileDescriptor
import java.util.function.Consumer
import java.util.function.Supplier

interface Communicator {
    abstract fun getMessageSender(): Consumer<String>
    abstract fun setOnNewMessageListener(onNewMessage: Consumer<String>)
    abstract fun getFileSender(): Consumer<FileDescriptor>
    abstract fun setOnNewFileListener(onNewFile: Supplier<FileDescriptor>)

    companion object {
        const val MAGIC_STRING_BYTE = 0x4D
        const val MAGIC_FILE_BYTE = 0x46
    }
}