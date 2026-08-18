// LiveKit WebRTC 视频会议 composable
// 后端签发短期房间令牌，浏览器直接连接自建 LiveKit，不再依赖 Agora。
import { markRaw, reactive, ref } from 'vue'
import { Room, RoomEvent, Track } from 'livekit-client'
import { leaveMeetingApi } from '@/api/meetings'

const state = ref('idle') // idle | joining | joined | reconnecting | leaving
const meetingId = ref(null)
const roomId = ref(null)
const channelName = ref(null)
const meetingTitle = ref('')
const serverUrl = ref(null)
const localAudioTrack = ref(null)
const localVideoTrack = ref(null)
const remoteUsers = reactive({})
const userMap = reactive({})
const localAudioEnabled = ref(true)
const localVideoEnabled = ref(true)
const errorMessage = ref('')

let roomClient = null
let joinSeq = 0
const leftTimers = new Map()
const remoteAudioElements = new Set()

function parseMetadata(metadata) {
  if (!metadata) return {}
  try {
    return JSON.parse(metadata)
  } catch {
    return {}
  }
}

function applyParticipantProfile(participant, fallback = {}) {
  if (!participant?.identity) return
  const metadata = parseMetadata(participant.metadata)
  userMap[participant.identity] = {
    userId: metadata.userId || fallback.userId || participant.identity.split(':')[0],
    userName: metadata.userName || metadata.username || participant.name ||
      fallback.userName || fallback.username || participant.identity,
    username: metadata.username || fallback.username || '',
    avatarUrl: metadata.avatarUrl || fallback.avatarUrl || null
  }
}

function ensureRemoteParticipant(participant) {
  const identity = participant?.identity
  if (!identity) return null

  const timer = leftTimers.get(identity)
  if (timer) {
    clearTimeout(timer)
    leftTimers.delete(identity)
  }

  if (!remoteUsers[identity]) {
    remoteUsers[identity] = {
      uid: identity,
      hasAudio: false,
      hasVideo: false,
      audioTrack: null,
      videoTrack: null
    }
  }
  applyParticipantProfile(participant, userMap[identity])
  return remoteUsers[identity]
}

function attachRemoteAudio(track) {
  if (typeof document === 'undefined' || !track) return
  try {
    const element = track.attach()
    element.autoplay = true
    element.dataset.livekitRemoteAudio = 'true'
    element.style.display = 'none'
    document.body.appendChild(element)
    remoteAudioElements.add(element)
    element.play?.().catch(() => {})
  } catch (error) {
    console.warn('[livekit] remote audio attach failed', error)
  }
}

function detachRemoteAudio(track) {
  if (!track) return
  try {
    const elements = track.detach?.() || []
    for (const element of elements) {
      remoteAudioElements.delete(element)
      element.remove?.()
    }
  } catch {}
}

function updatePublicationState(publication, participant, enabled) {
  const remote = ensureRemoteParticipant(participant)
  if (!remote) return
  if (publication.source === Track.Source.Microphone || publication.kind === Track.Kind.Audio) {
    remote.hasAudio = enabled
  } else if (publication.source === Track.Source.Camera || publication.kind === Track.Kind.Video) {
    remote.hasVideo = enabled
  }
}

function registerRoomEvents(room, seq) {
  room.on(RoomEvent.ParticipantConnected, (participant) => {
    if (seq === joinSeq) ensureRemoteParticipant(participant)
  })

  room.on(RoomEvent.ParticipantDisconnected, (participant) => {
    if (seq !== joinSeq) return
    const identity = participant.identity
    const previous = leftTimers.get(identity)
    if (previous) clearTimeout(previous)
    leftTimers.set(identity, setTimeout(() => {
      leftTimers.delete(identity)
      if (seq === joinSeq) {
        remoteUsers[identity]?.videoTrack?.detach?.()
        detachRemoteAudio(remoteUsers[identity]?.audioTrack)
        delete remoteUsers[identity]
        delete userMap[identity]
      }
    }, 1200))
  })

  room.on(RoomEvent.TrackSubscribed, (track, publication, participant) => {
    if (seq !== joinSeq) return
    const remote = ensureRemoteParticipant(participant)
    if (!remote) return
    if (track.kind === Track.Kind.Audio) {
      remote.audioTrack = markRaw(track)
      remote.hasAudio = !publication.isMuted
      attachRemoteAudio(track)
    } else if (track.kind === Track.Kind.Video) {
      remote.videoTrack = markRaw(track)
      remote.hasVideo = !publication.isMuted
    }
  })

  room.on(RoomEvent.TrackUnsubscribed, (track, _publication, participant) => {
    if (seq !== joinSeq) return
    const remote = remoteUsers[participant.identity]
    if (track.kind === Track.Kind.Audio) detachRemoteAudio(track)
    else try { track.detach?.() } catch {}
    if (!remote) return
    if (track.kind === Track.Kind.Audio) {
      remote.audioTrack = null
      remote.hasAudio = false
    } else if (track.kind === Track.Kind.Video) {
      remote.videoTrack = null
      remote.hasVideo = false
    }
  })

  room.on(RoomEvent.TrackMuted, (publication, participant) => {
    if (seq === joinSeq) updatePublicationState(publication, participant, false)
  })
  room.on(RoomEvent.TrackUnmuted, (publication, participant) => {
    if (seq === joinSeq) updatePublicationState(publication, participant, true)
  })
  room.on(RoomEvent.ParticipantMetadataChanged, (_metadata, participant) => {
    if (seq === joinSeq) applyParticipantProfile(participant, userMap[participant.identity])
  })
  room.on(RoomEvent.ParticipantNameChanged, (_name, participant) => {
    if (seq === joinSeq) applyParticipantProfile(participant, userMap[participant.identity])
  })
  room.on(RoomEvent.Reconnecting, () => {
    if (seq === joinSeq) state.value = 'reconnecting'
  })
  room.on(RoomEvent.Reconnected, () => {
    if (seq === joinSeq) {
      state.value = 'joined'
      errorMessage.value = ''
    }
  })
  room.on(RoomEvent.Disconnected, () => {
    if (seq === joinSeq && state.value !== 'leaving') {
      const disconnectMessage = '会议连接已断开，请重新加入'
      cleanup()
      state.value = 'idle'
      errorMessage.value = disconnectMessage
    }
  })
  room.on(RoomEvent.MediaDevicesError, (error) => {
    errorMessage.value = `无法访问音视频设备：${error?.message || '请检查浏览器权限'}`
  })
}

function seedMemberProfiles(joinPayload) {
  const members = joinPayload.meeting?.members || []
  for (const member of members) {
    const profile = {
      userId: member.userId,
      userName: member.userName || member.username || '参会人',
      username: member.username || '',
      avatarUrl: member.avatarUrl || null
    }
    for (const liveKitIdentity of (member.liveKitIdentities || [])) {
      if (liveKitIdentity?.identity) userMap[liveKitIdentity.identity] = profile
    }
  }
}

function hydrateRemoteParticipants(room) {
  for (const participant of room.remoteParticipants.values()) {
    const remote = ensureRemoteParticipant(participant)
    if (!remote) continue
    for (const publication of participant.trackPublications.values()) {
      const track = publication.track
      if (!track) continue
      if (track.kind === Track.Kind.Audio) {
        remote.audioTrack = markRaw(track)
        remote.hasAudio = !publication.isMuted
        attachRemoteAudio(track)
      } else if (track.kind === Track.Kind.Video) {
        remote.videoTrack = markRaw(track)
        remote.hasVideo = !publication.isMuted
      }
    }
  }
}

async function forceReset() {
  if (meetingId.value) {
    try { await leaveMeetingApi(meetingId.value).catch(() => {}) } catch {}
  }
  joinSeq++
  try { await roomClient?.disconnect(true) } catch {}
  cleanup()
}

async function joinMeeting(joinPayload) {
  if (state.value !== 'idle') await forceReset()

  const wsUrl = joinPayload.serverUrl || joinPayload.wsUrl
  const token = joinPayload.accessToken || joinPayload.participantToken
  const roomName = joinPayload.roomName || joinPayload.channelName
  if (!wsUrl || !token || !roomName) {
    throw new Error('LiveKit 入会参数不完整，请检查后端 LiveKit 配置')
  }

  const seq = ++joinSeq
  state.value = 'joining'
  errorMessage.value = ''
  serverUrl.value = wsUrl
  channelName.value = roomName
  roomId.value = joinPayload.roomId
  meetingId.value = joinPayload.meeting?.id
  meetingTitle.value = joinPayload.meeting?.title || ''
  localAudioEnabled.value = true
  localVideoEnabled.value = joinPayload.meeting?.hasVideo !== false
  seedMemberProfiles(joinPayload)

  const room = markRaw(new Room({
    adaptiveStream: true,
    dynacast: true,
    disconnectOnPageLeave: true
  }))
  roomClient = room
  registerRoomEvents(room, seq)

  try {
    await room.connect(wsUrl, token)
    if (seq !== joinSeq) {
      await room.disconnect(true)
      throw new DOMException('加入会议操作已取消', 'AbortError')
    }
    hydrateRemoteParticipants(room)

    try {
      const publication = await room.localParticipant.setMicrophoneEnabled(true)
      localAudioTrack.value = publication?.track ? markRaw(publication.track) : null
      localAudioEnabled.value = Boolean(publication)
    } catch (error) {
      localAudioEnabled.value = false
      console.warn('[livekit] microphone unavailable', error)
    }

    if (localVideoEnabled.value) {
      try {
        const publication = await room.localParticipant.setCameraEnabled(true)
        localVideoTrack.value = publication?.track ? markRaw(publication.track) : null
        localVideoEnabled.value = Boolean(publication)
      } catch (error) {
        localVideoEnabled.value = false
        console.warn('[livekit] camera unavailable', error)
      }
    }

    if (seq === joinSeq) state.value = 'joined'
  } catch (error) {
    console.error('[livekit] join failed', error)
    errorMessage.value = error?.message || '加入会议失败'
    if (seq === joinSeq) {
      try { await room.disconnect(true) } catch {}
      cleanup()
      state.value = 'idle'
    }
    throw error
  }
}

async function leaveMeeting() {
  if (state.value === 'idle') return
  state.value = 'leaving'
  joinSeq++
  try {
    await Promise.race([
      roomClient?.disconnect(true),
      new Promise((_, reject) => setTimeout(() => reject(new Error('leave timeout')), 3000))
    ])
  } catch (error) {
    console.warn('[livekit] leave error/timeout', error)
  }
  cleanup()
  state.value = 'idle'
}

function cleanup() {
  try { localAudioTrack.value?.detach?.() } catch {}
  try { localVideoTrack.value?.detach?.() } catch {}
  for (const remote of Object.values(remoteUsers)) {
    detachRemoteAudio(remote.audioTrack)
    try { remote.videoTrack?.detach?.() } catch {}
  }
  for (const element of remoteAudioElements) {
    try { element.remove() } catch {}
  }
  remoteAudioElements.clear()
  for (const timer of leftTimers.values()) clearTimeout(timer)
  leftTimers.clear()

  localAudioTrack.value = null
  localVideoTrack.value = null
  localAudioEnabled.value = true
  localVideoEnabled.value = true
  serverUrl.value = null
  channelName.value = null
  roomId.value = null
  meetingId.value = null
  meetingTitle.value = ''
  for (const identity of Object.keys(remoteUsers)) delete remoteUsers[identity]
  for (const identity of Object.keys(userMap)) delete userMap[identity]
  roomClient?.removeAllListeners?.()
  roomClient = null
}

async function toggleMute() {
  if (!roomClient) return
  const nextEnabled = !localAudioEnabled.value
  try {
    const publication = await roomClient.localParticipant.setMicrophoneEnabled(nextEnabled)
    localAudioTrack.value = publication?.track ? markRaw(publication.track) : null
    localAudioEnabled.value = nextEnabled
    errorMessage.value = ''
  } catch (error) {
    errorMessage.value = `切换麦克风失败：${error?.message || ''}`
  }
}

async function toggleCamera() {
  if (!roomClient) return
  const nextEnabled = !localVideoEnabled.value
  try {
    const publication = await roomClient.localParticipant.setCameraEnabled(nextEnabled)
    localVideoTrack.value = publication?.track ? markRaw(publication.track) : null
    localVideoEnabled.value = nextEnabled
    errorMessage.value = ''
  } catch (error) {
    errorMessage.value = `切换摄像头失败：${error?.message || '请检查设备权限'}`
  }
}

export function useLiveKit() {
  return {
    state,
    meetingId,
    roomId,
    channelName,
    meetingTitle,
    serverUrl,
    localAudioTrack,
    localVideoTrack,
    remoteUsers,
    userMap,
    localAudioEnabled,
    localVideoEnabled,
    errorMessage,
    joinMeeting,
    leaveMeeting,
    toggleMute,
    toggleCamera
  }
}
