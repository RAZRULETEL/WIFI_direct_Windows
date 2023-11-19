package com.mastik.wifidirect.tasks

import java.util.concurrent.ExecutorService
import java.util.concurrent.SynchronousQueue
import java.util.concurrent.ThreadPoolExecutor
import java.util.concurrent.TimeUnit


object TaskExecutors {
        private const val FIXED_THREADS_COUNT = 2
        private const val MAX_THREADS_COUNT = 20

        private val cachedPool = ThreadPoolExecutor(// Not real cached, but similar
            FIXED_THREADS_COUNT, MAX_THREADS_COUNT,
            100L, TimeUnit.SECONDS,
            SynchronousQueue()
        )

        fun getCachedPool(): ExecutorService {
            return cachedPool
        }
}