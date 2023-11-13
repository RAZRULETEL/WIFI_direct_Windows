package com.mastik.lib_test.tasks

import java.util.function.Consumer

interface Communicator {
    abstract fun getMessageSender(): Consumer<String>
    abstract fun setOnNewMessageListener(onNewMessage: Consumer<String>)
}