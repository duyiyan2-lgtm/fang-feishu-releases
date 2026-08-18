package com.fangfeishu.android.rtc

import android.content.Context
import android.os.Handler
import android.os.Looper
import android.view.SurfaceView
import android.view.ViewGroup
import android.widget.FrameLayout
import com.fangfeishu.android.data.MeetingJoinData
import io.agora.rtc2.ChannelMediaOptions
import io.agora.rtc2.Constants
import io.agora.rtc2.IRtcEngineEventHandler
import io.agora.rtc2.RtcEngine
import io.agora.rtc2.video.VideoCanvas

data class RemoteRtcParticipant(
    val uid: Int,
    val muted: Boolean = false,
    val cameraEnabled: Boolean = true
)

class AgoraRtcManager(context: Context) {
    private val appContext: Context = context.applicationContext ?: context
    private val mainHandler = Handler(Looper.getMainLooper())
    private var engine: RtcEngine? = null
    private var localContainer: FrameLayout? = null
    private val remoteContainers = mutableMapOf<Int, FrameLayout>()
    private val remoteViews = mutableMapOf<Int, SurfaceView>()
    private val remoteParticipants = linkedMapOf<Int, RemoteRtcParticipant>()

    var onRemoteParticipantsChanged: ((List<RemoteRtcParticipant>) -> Unit)? = null
        set(value) {
            field = value
            value?.invoke(remoteParticipants.values.toList())
        }

    private val eventHandler = object : IRtcEngineEventHandler() {
        override fun onUserJoined(uid: Int, elapsed: Int) {
            mainHandler.post {
                remoteParticipants.putIfAbsent(uid, RemoteRtcParticipant(uid))
                publishRemoteParticipants()
                attachRemote(uid)
            }
        }

        override fun onUserOffline(uid: Int, reason: Int) {
            mainHandler.post {
                remoteParticipants.remove(uid)
                detachRemote(uid)
                publishRemoteParticipants()
            }
        }

        override fun onUserMuteAudio(uid: Int, muted: Boolean) {
            mainHandler.post {
                updateRemoteParticipant(uid) { it.copy(muted = muted) }
            }
        }

        override fun onUserMuteVideo(uid: Int, muted: Boolean) {
            mainHandler.post {
                updateRemoteParticipant(uid) { it.copy(cameraEnabled = !muted) }
            }
        }

        override fun onRemoteVideoStateChanged(uid: Int, state: Int, reason: Int, elapsed: Int) {
            mainHandler.post {
                when (state) {
                    Constants.REMOTE_VIDEO_STATE_DECODING ->
                        updateRemoteParticipant(uid) { it.copy(cameraEnabled = true) }
                    Constants.REMOTE_VIDEO_STATE_STOPPED,
                    Constants.REMOTE_VIDEO_STATE_FAILED ->
                        updateRemoteParticipant(uid) { it.copy(cameraEnabled = false) }
                }
            }
        }
    }

    fun join(joinData: MeetingJoinData, local: FrameLayout) {
        if (engine != null) return
        require(joinData.uid in 1..Int.MAX_VALUE.toLong()) {
            "服务器生成的会议 UID 超出 Android 支持范围，请先部署最新后端"
        }
        localContainer = local

        val rtc = RtcEngine.create(appContext, joinData.appId, eventHandler)
        engine = rtc
        try {
            rtc.enableVideo()
            rtc.setClientRole(Constants.CLIENT_ROLE_BROADCASTER)
            attachLocal(rtc)
            rtc.startPreview()
            val result = rtc.joinChannel(
                joinData.rtcToken?.takeIf { it.isNotBlank() },
                joinData.channelName,
                joinData.uid.toInt(),
                ChannelMediaOptions().apply {
                    channelProfile = Constants.CHANNEL_PROFILE_COMMUNICATION
                    clientRoleType = Constants.CLIENT_ROLE_BROADCASTER
                    publishCameraTrack = true
                    publishMicrophoneTrack = true
                    autoSubscribeAudio = true
                    autoSubscribeVideo = true
                }
            )
            check(result == 0) { "Agora 加入会议失败（错误码 $result）" }
        } catch (error: Throwable) {
            leave()
            throw error
        }
    }

    fun setMuted(muted: Boolean) {
        engine?.muteLocalAudioStream(muted)
    }

    fun setCameraEnabled(enabled: Boolean) {
        engine?.muteLocalVideoStream(!enabled)
        engine?.enableLocalVideo(enabled)
        if (enabled) engine?.startPreview() else engine?.stopPreview()
    }

    fun bindRemoteContainer(uid: Int, container: FrameLayout) {
        remoteContainers[uid] = container
        attachRemote(uid)
    }

    fun leave() {
        engine?.stopPreview()
        engine?.leaveChannel()
        RtcEngine.destroy()
        engine = null
        localContainer = null
        remoteContainers.values.forEach { it.removeAllViews() }
        remoteContainers.clear()
        remoteViews.clear()
        remoteParticipants.clear()
        publishRemoteParticipants()
    }

    private fun attachLocal(rtc: RtcEngine) {
        val container = localContainer ?: return
        container.removeAllViews()
        val view = SurfaceView(appContext)
        container.addView(view, FrameLayout.LayoutParams(FrameLayout.LayoutParams.MATCH_PARENT, FrameLayout.LayoutParams.MATCH_PARENT))
        rtc.setupLocalVideo(VideoCanvas(view, Constants.RENDER_MODE_HIDDEN, 0))
    }

    private fun attachRemote(uid: Int) {
        if (uid !in remoteParticipants) return
        val container = remoteContainers[uid] ?: return
        val rtc = engine ?: return
        detachRemoteView(uid)
        container.removeAllViews()
        val view = SurfaceView(appContext)
        container.addView(view, FrameLayout.LayoutParams(FrameLayout.LayoutParams.MATCH_PARENT, FrameLayout.LayoutParams.MATCH_PARENT))
        remoteViews[uid] = view
        rtc.setupRemoteVideo(VideoCanvas(view, Constants.RENDER_MODE_HIDDEN, uid))
    }

    private fun detachRemote(uid: Int) {
        detachRemoteView(uid)
        remoteContainers.remove(uid)?.removeAllViews()
    }

    private fun detachRemoteView(uid: Int) {
        remoteViews.remove(uid)?.let { view ->
            (view.parent as? ViewGroup)?.removeView(view)
        }
    }

    private fun updateRemoteParticipant(uid: Int, update: (RemoteRtcParticipant) -> RemoteRtcParticipant) {
        val current = remoteParticipants[uid] ?: RemoteRtcParticipant(uid)
        remoteParticipants[uid] = update(current)
        publishRemoteParticipants()
    }

    private fun publishRemoteParticipants() {
        onRemoteParticipantsChanged?.invoke(remoteParticipants.values.toList())
    }
}
