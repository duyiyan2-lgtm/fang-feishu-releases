package com.fangfeishu.android.realtime

import com.fangfeishu.android.data.IM_HUB_URL
import com.fangfeishu.android.data.SessionHolder
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.microsoft.signalr.HubConnectionState
import io.reactivex.rxjava3.core.Single
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.NonCancellable
import kotlinx.coroutines.withContext

/**
 * Keeps the active chat screen subscribed to the server-side IM SignalR hub.
 * Message bodies are still loaded through the REST API so this client does not
 * need to duplicate the server's message deserialization model.
 */
class ImRealtimeClient {
    private var connection: HubConnection? = null

    suspend fun connect(onMessageReceived: () -> Unit): Result<Unit> = withContext(Dispatchers.IO) {
        if (connection?.connectionState == HubConnectionState.CONNECTED) {
            return@withContext Result.success(Unit)
        }

        runCatching {
            stopConnection()
            val initialToken = SessionHolder.token
                ?: error("登录已失效，请重新登录")
            val newConnection = HubConnectionBuilder.create(IM_HUB_URL)
                .withAccessTokenProvider(
                    Single.defer {
                        Single.just(SessionHolder.token ?: initialToken)
                    }
                )
                .build()

            newConnection.on(
                "ReceiveMessage",
                { _: Any -> onMessageReceived() },
                Any::class.java
            )
            newConnection.start().blockingAwait()
            connection = newConnection
        }
    }

    suspend fun disconnect() = withContext(NonCancellable + Dispatchers.IO) {
        stopConnection()
    }

    private fun stopConnection() {
        val activeConnection = connection ?: return
        connection = null
        runCatching { activeConnection.stop().blockingAwait() }
    }
}
