// Agora 声网 WebRTC 视频通话 composable
// - 后端通过 MeetingsController 提供 token
// - 前端用 agora-rtc-sdk-ng 加入声网频道
// - 媒体流走声网服务器（不消耗本地带宽）
import { ref, reactive, onUnmounted, markRaw } from 'vue'
import AgoraRTC from 'agora-rtc-sdk-ng'

// 单例 state（同一页面/同标签页内复用 Agora client）
const state = ref('idle')                  // idle | joining | joined | leaving
const meetingId = ref(null)
const roomId = ref(null)
const channelName = ref(null)
const meetingTitle = ref('')
const appId = ref(null)
const localAudioTrack = ref(null)
const localVideoTrack = ref(null)
const remoteUsers = reactive({})            // uid -> { audioTrack, videoTrack, hasAudio, hasVideo }
// rtc uid -> { userId, userName, username, avatarUrl }（从 meeting.members.rtcIdentities 构建）
const userMap = reactive({})
const localAudioEnabled = ref(true)
const localVideoEnabled = ref(true)
const errorMessage = ref('')
// markRaw：Agora client 内部状态很多，Vue 不应把它变 reactive（也避免重复代理）
let client = null
let joinSeq = 0   // 单调递增的加入序号，旧的 join 完成后回调若已过期则丢弃
const leftTimers = new Map()

/** 创建 Agora client（每个会议一个） */
function createClient() {
  return AgoraRTC.createClient({ mode: 'rtc', codec: 'h264' })
}

/** 强制清理状态：先 leave，再清 track，再清远端 */
async function forceReset() {
  // 1. 调后端 leave 接口（解绑 uid，避免下次 join 撞 UID_CONFLICT）
  if (meetingId.value) {
    try {
      const { leaveMeetingApi } = await import('@/api/meetings')
      await leaveMeetingApi(meetingId.value).catch(() => {})
    } catch {}
  }
  // 2. 断开 Agora 连接
  try {
    if (client) {
      await Promise.race([
        client.leave().catch(() => {}),
        new Promise((_, reject) => setTimeout(() => reject(new Error('leave timeout')), 2000))
      ])
    }
  } catch {}
  // 3. 清本地状态
  await cleanup()
}

/** 加入会议 */
async function joinMeeting(joinPayload) {
  // 如果已经有会议，先强制清理（修复：旧的 singleton 卡死状态导致 rejoin 直接 return）
  if (state.value !== 'idle') {
    await forceReset()
  }
  const mySeq = ++joinSeq
  state.value = 'joining'
  errorMessage.value = ''
  try {
    const { appId: aId, channelName: ch, uid, rtcToken } = joinPayload
    appId.value = aId
    channelName.value = ch
    roomId.value = joinPayload.roomId
    meetingId.value = joinPayload.meeting?.id
    meetingTitle.value = joinPayload.meeting?.title || ''

    // 从 meeting.members 构建 rtc uid → 用户信息 的映射
    const members = joinPayload.meeting?.members || []
    const { useUserStore } = await import('@/stores/user')
    const meId = useUserStore().userInfo?.id
    for (const m of members) {
      for (const ri of (m.rtcIdentities || [])) {
        if (ri?.uid != null) {
          userMap[String(ri.uid)] = {
            userId: m.userId,
            userName: m.userName || m.username || '远端',
            username: m.username || '',
            avatarUrl: m.avatarUrl || null
          }
        }
      }
    }
    // 自己也加进去（user-published 不会包含自己）
    if (uid != null && meId && !userMap[String(uid)]) {
      userMap[String(uid)] = {
        userId: meId,
        userName: useUserStore().userInfo?.realName || useUserStore().userInfo?.username || '我',
        username: useUserStore().userInfo?.username || '',
        avatarUrl: null
      }
    }

    // 创建 Agora client
    const c = markRaw(createClient())
    client = c

    // 注册远端用户事件
    c.on('user-published', async (user, mediaType) => {
      if (mySeq !== joinSeq) return  // 旧 join 的回调，直接丢弃
      try {
        await c.subscribe(user, mediaType)
      } catch (e) {
        console.warn('[agora] subscribe failed', e)
        return
      }
      if (mySeq !== joinSeq) return
      const key = String(user.uid)
      const timer = leftTimers.get(key)
      if (timer) {
        clearTimeout(timer)
        leftTimers.delete(key)
      }
      if (!remoteUsers[user.uid]) {
        remoteUsers[user.uid] = { hasAudio: false, hasVideo: false, uid: user.uid }
      } else {
        // 已有：兜底初始化（防御代码）
        if (typeof remoteUsers[user.uid].hasAudio !== 'boolean') remoteUsers[user.uid].hasAudio = false
        if (typeof remoteUsers[user.uid].hasVideo !== 'boolean') remoteUsers[user.uid].hasVideo = false
      }
      if (mediaType === 'audio') {
        remoteUsers[user.uid].audioTrack = user.audioTrack
        remoteUsers[user.uid].hasAudio = true
        try { await user.audioTrack?.play?.() } catch {}
      } else if (mediaType === 'video') {
        remoteUsers[user.uid].videoTrack = user.videoTrack
        remoteUsers[user.uid].hasVideo = true
      }
    })

    c.on('user-unpublished', (user, mediaType) => {
      if (mySeq !== joinSeq) return
      if (mediaType === 'audio') {
        if (remoteUsers[user.uid]) remoteUsers[user.uid].hasAudio = false
      } else if (mediaType === 'video') {
        if (remoteUsers[user.uid]) remoteUsers[user.uid].hasVideo = false
      }
    })

    c.on('user-left', (user) => {
      if (mySeq !== joinSeq) return
      const key = String(user.uid)
      const prev = leftTimers.get(key)
      if (prev) clearTimeout(prev)
      const timer = setTimeout(() => {
        leftTimers.delete(key)
        if (mySeq === joinSeq) delete remoteUsers[user.uid]
      }, 1200)
      leftTimers.set(key, timer)
    })

    // 加入频道
    await c.join(aId, ch, rtcToken || null, uid)
    if (mySeq !== joinSeq) return  // 期间被新的 join 替换

    // 创建并发布本地音视频
    const [audioTrack, videoTrack] = await Promise.all([
      AgoraRTC.createMicrophoneAudioTrack().catch((e) => {
        console.error('[agora] createMicrophoneAudioTrack failed', e)
        return null
      }),
      joinPayload.meeting?.hasVideo !== false
        ? AgoraRTC.createCameraVideoTrack().catch((e) => {
            console.warn('[agora] createCameraVideoTrack failed (no camera?)', e)
            return null
          })
        : Promise.resolve(null)
    ])
    if (mySeq !== joinSeq) {
      // 又被替换了，把刚创建的 track 释放
      try { audioTrack?.stop?.(); audioTrack?.close?.() } catch {}
      try { videoTrack?.stop?.(); videoTrack?.close?.() } catch {}
      return
    }
    localAudioTrack.value = audioTrack
    localVideoTrack.value = videoTrack
    const tracksToPublish = [audioTrack, videoTrack].filter(Boolean)
    if (tracksToPublish.length) {
      await c.publish(tracksToPublish)
    }

    state.value = 'joined'
  } catch (e) {
    console.error('[agora] joinMeeting failed', e)
    errorMessage.value = e.message || '加入会议失败'
    if (mySeq === joinSeq) {
      await cleanup()
      state.value = 'idle'
    }
    throw e
  }
}

/** 离开会议 — 加 3s 超时，绝不卡住 */
async function leaveMeeting() {
  if (state.value === 'idle') return
  state.value = 'leaving'
  joinSeq++  // 令所有未完成的回调过期
  try {
    if (client) {
      await Promise.race([
        client.leave(),
        new Promise((_, reject) => setTimeout(() => reject(new Error('leave timeout')), 3000))
      ])
    }
  } catch (e) {
    console.warn('[agora] leave error/timeout:', e.message)
  }
  await cleanup()
  state.value = 'idle'
}

async function cleanup() {
  // 1. 关闭本地 track
  try { localAudioTrack.value?.stop() } catch {}
  try { localVideoTrack.value?.stop() } catch {}
  try { localAudioTrack.value?.close() } catch {}
  try { localVideoTrack.value?.close() } catch {}

  // 2. 清所有 <video> 元素的 srcObject（关键：黑屏修复）
  for (const el of (typeof document !== 'undefined' ? document.querySelectorAll('video') : [])) {
    try { el.srcObject = null } catch {}
  }

  // 3. 清状态
  localAudioTrack.value = null
  localVideoTrack.value = null
  localAudioEnabled.value = true
  localVideoEnabled.value = true
  appId.value = null
  channelName.value = null
  roomId.value = null
  meetingId.value = null
  meetingTitle.value = ''

  // 4. 清远端用户 + 用户映射
  for (const timer of leftTimers.values()) clearTimeout(timer)
  leftTimers.clear()
  for (const uid of Object.keys(remoteUsers)) delete remoteUsers[uid]
  for (const uid of Object.keys(userMap)) delete userMap[uid]

  // 5. 移除 listeners + 关 client
  if (client) {
    try { client.removeAllListeners?.() } catch {}
  }
  client = null
}

/** 静音 / 取消 */
async function toggleMute() {
  if (!localAudioTrack.value) return
  localAudioEnabled.value = !localAudioEnabled.value
  await localAudioTrack.value.setEnabled(localAudioEnabled.value)
}

/** 开关摄像头 —— 用 track.setEnabled() 而不是 unpublish/republish
 *  原因：Agora 服务器处理 unpublish 有延迟，立即 publish 新 track 会撞
 *  "CAN_NOT_PUBLISH_MULTIPLE_VIDEO_TRACKS"。setEnabled 是黑/白视频流的官方方式，
 *  无需重新发布，无竞态，切换瞬时。
 *  副作用：摄像头灯会一直亮（设备未释放）。如需彻底释放设备，
 *  可加一个 "退出会议时" 调 localVideoTrack.close()。
 */
async function toggleCamera() {
  if (!client) return
  const turningOn = !localVideoEnabled.value
  localVideoEnabled.value = turningOn

  // 情况 1：首次开启（join 时没建 video track）
  if (turningOn && !localVideoTrack.value) {
    let track = null
    try {
      track = await AgoraRTC.createCameraVideoTrack()
    } catch (e) {
      console.error('[agora] createCameraVideoTrack failed', e)
      localVideoEnabled.value = false
      errorMessage.value = '无法打开摄像头：' + (e?.message || '设备被占用')
      return
    }
    localVideoTrack.value = track
    try {
      await client.publish(track)
    } catch (e) {
      console.error('[agora] publish video track failed', e)
      try { track.stop() } catch {}
      try { track.close() } catch {}
      localVideoTrack.value = null
      localVideoEnabled.value = false
      errorMessage.value = '发布视频失败：' + (e?.message || '')
      return
    }
    // 绑定到本地预览元素
    try {
      const localEl = document.querySelector('[data-local-video]')
      if (localEl) await track.play(localEl, { fit: 'cover', mirror: false })
    } catch (e) { /* watcher 兜底 */ }
    errorMessage.value = ''
    return
  }

  // 情况 2：已有 track，用 setEnabled 切换（核心修复点）
  if (localVideoTrack.value) {
    try {
      await localVideoTrack.value.setEnabled(turningOn)
    } catch (e) {
      console.error('[agora] setEnabled video failed', e)
      errorMessage.value = '切换摄像头失败：' + (e?.message || '')
      localVideoEnabled.value = !turningOn  // 回滚
      return
    }
    if (!turningOn) {
      // 关：清本地 preview（不影响远端）
      try {
        const localEl = document.querySelector('[data-local-video]')
        if (localEl) localEl.srcObject = null
      } catch {}
    } else {
      // 开：重新绑定到本地元素（关键！之前 track.play 只调一次，重新开时元素需要重新挂）
      try {
        const localEl = document.querySelector('[data-local-video]')
        if (localEl) await localVideoTrack.value.play(localEl, { fit: 'cover', mirror: false })
      } catch (e) { /* watcher 兜底 */ }
    }
    errorMessage.value = ''
  }
}

export function useAgora() {
  // 离开时自动清理（onUnmounted 必须在 setup 函数内）
  onUnmounted(() => {
    if (state.value !== 'idle') leaveMeeting()
  })

  return {
    state, meetingId, roomId, channelName, meetingTitle, appId,
    localAudioTrack, localVideoTrack, remoteUsers, userMap,
    localAudioEnabled, localVideoEnabled, errorMessage,
    joinMeeting, leaveMeeting, toggleMute, toggleCamera
  }
}