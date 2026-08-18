<template>
  <Teleport to="body">
    <transition name="meeting-fade" appear>
      <div v-if="visible" ref="roomRoot" class="meeting-room">
        <header class="meeting-header">
          <div class="meeting-title-block">
            <button class="header-back" title="返回消息" @click="handleEnd">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="m15 18-6-6 6-6" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
            </button>
            <div>
              <div class="meeting-title-line">
                <h1>{{ meetingTitle || '即时视频会议' }}</h1>
                <span class="meeting-live"><i></i>{{ stateLabel }}</span>
              </div>
              <p>LiveKit RTC <span>·</span> {{ channelName || '安全会议房间' }} <span>·</span> {{ formatDuration(duration) }}</p>
            </div>
          </div>
          <div class="meeting-header-actions">
            <PerformanceIndicator compact />
            <span class="participant-count">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm13 10v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" stroke-width="1.8" stroke-linecap="round"/></svg>
              {{ remoteUserCount + 1 }} 人
            </span>
            <button class="header-action" title="全屏" @click="toggleFullscreen">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M8 3H5a2 2 0 0 0-2 2v3m18 0V5a2 2 0 0 0-2-2h-3m0 18h3a2 2 0 0 0 2-2v-3M3 16v3a2 2 0 0 0 2 2h3" stroke-width="1.8" stroke-linecap="round"/></svg>
            </button>
          </div>
        </header>

        <main class="meeting-stage">
          <div class="stage-glow glow-one"></div><div class="stage-glow glow-two"></div>
          <div class="media-grid" :style="gridStyle">
            <article class="participant-tile is-local" :class="{ 'camera-on': localVideoEnabled }">
              <video v-show="localVideoTrack && localVideoEnabled" ref="localVideoEl" data-local-video autoplay playsinline muted></video>
              <div v-if="!localVideoTrack || !localVideoEnabled" class="camera-placeholder">
                <div class="avatar avatar-local">
                  <img v-if="localAvatar" :src="localAvatar" alt="我的头像" />
                  <span v-else>{{ localInitial }}</span>
                </div>
                <strong>{{ localVideoEnabled ? '正在打开摄像头' : '摄像头已关闭' }}</strong>
                <small>{{ localAudioEnabled ? '麦克风已开启' : '当前已静音' }}</small>
              </div>
              <div class="tile-topline"><span>我</span><em>本机</em></div>
              <div class="tile-info">
                <span class="tile-name">{{ localName }}</span>
                <span v-if="!localAudioEnabled" class="media-off" title="已静音">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="m2 2 20 20M9 9v3a3 3 0 0 0 5.12 2.12M15 9.34V5a3 3 0 0 0-5.94-.6M17 16.95A7 7 0 0 1 5 12v-2m14 0v2a7 7 0 0 1-.11 1.23M12 19v3m-4 0h8" stroke-width="1.8" stroke-linecap="round"/></svg>
                </span>
              </div>
            </article>

            <article v-for="(remote, uid) in remoteUsers" :key="`remote-${uid}`" class="participant-tile" :class="{ 'camera-on': remote.hasVideo }">
              <video v-show="remote.videoTrack && remote.hasVideo" :ref="el => bindRemoteVideo(el, uid)" autoplay playsinline></video>
              <div v-if="!remote.videoTrack || !remote.hasVideo" class="camera-placeholder">
                <div class="avatar">
                  <img v-if="userMap[uid]?.avatarUrl" :src="userMap[uid].avatarUrl" alt="参会人头像" />
                  <span v-else>{{ participantInitial(uid) }}</span>
                </div>
                <strong>摄像头已关闭</strong>
                <small>{{ remote.hasAudio ? '仅使用语音加入' : '当前已静音' }}</small>
              </div>
              <div class="tile-info">
                <span class="tile-name">{{ participantName(uid) }}</span>
                <span v-if="!remote.hasAudio" class="media-off" title="已静音">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="m2 2 20 20M9 9v3a3 3 0 0 0 5.12 2.12M15 9.34V5a3 3 0 0 0-5.94-.6M17 16.95A7 7 0 0 1 5 12v-2m14 0v2a7 7 0 0 1-.11 1.23M12 19v3m-4 0h8" stroke-width="1.8" stroke-linecap="round"/></svg>
                </span>
              </div>
            </article>

            <article v-if="remoteUserCount === 0" class="participant-tile waiting-tile">
              <div class="waiting-icon"><span></span><span></span><span></span></div>
              <strong>等待参会人加入</strong>
              <small>邀请成员后，他们会出现在这里</small>
            </article>
          </div>
        </main>

        <transition name="error-slide">
          <div v-if="errorMessage" class="meeting-error">
            <span>!</span><div><strong>连接提示</strong><p>{{ errorMessage }}</p></div>
            <button @click="errorMessage = ''">×</button>
          </div>
        </transition>

        <footer class="meeting-footer">
          <div class="control-dock">
            <button class="control-button" :class="{ off: !localAudioEnabled }" @click="toggleMute">
              <span>
                <svg v-if="localAudioEnabled" viewBox="0 0 24 24" fill="none" stroke="currentColor"><rect x="9" y="2" width="6" height="12" rx="3" stroke-width="1.8"/><path d="M5 10v1a7 7 0 0 0 14 0v-1M12 18v4m-4 0h8" stroke-width="1.8" stroke-linecap="round"/></svg>
                <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="m2 2 20 20M9 9v3a3 3 0 0 0 5.12 2.12M15 9.34V5a3 3 0 0 0-5.94-.6M17 16.95A7 7 0 0 1 5 12v-2m14 0v2a7 7 0 0 1-.11 1.23M12 19v3m-4 0h8" stroke-width="1.8" stroke-linecap="round"/></svg>
              </span>
              <em>{{ localAudioEnabled ? '静音' : '取消静音' }}</em>
            </button>
            <button class="control-button" :class="{ off: !localVideoEnabled }" @click="toggleCamera">
              <span>
                <svg v-if="localVideoEnabled" viewBox="0 0 24 24" fill="none" stroke="currentColor"><rect x="3" y="6" width="13" height="12" rx="3" stroke-width="1.8"/><path d="m16 10 5-3v10l-5-3" stroke-width="1.8" stroke-linejoin="round"/></svg>
                <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="m2 2 20 20M15 10.3V9a3 3 0 0 0-3-3H8.7M16 16.2 21 19V9l-5 2.8M13 18H6a3 3 0 0 1-3-3V9c0-.7.24-1.34.65-1.85" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>
              </span>
              <em>{{ localVideoEnabled ? '关闭摄像头' : '打开摄像头' }}</em>
            </button>
            <div class="dock-separator"></div>
            <button class="control-button hangup" @click="handleEnd">
              <span><svg viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M4.7 15.5c4.6-3.3 10-3.3 14.6 0M5 15l-2 3m16-3 2 3" stroke-width="2.2" stroke-linecap="round"/></svg></span>
              <em>离开会议</em>
            </button>
          </div>
        </footer>
      </div>
    </transition>
  </Teleport>
</template>

<script setup>
import { computed, nextTick, onUnmounted, ref, watch } from 'vue'
import { useLiveKit } from '@/composables/useLiveKit'
import { useMeetingStore } from '@/stores/meeting'
import { useUserStore } from '@/stores/user'
import PerformanceIndicator from '@/components/PerformanceIndicator.vue'

const props = defineProps({ visible: Boolean })
const emit = defineEmits(['update:visible', 'ended'])
const meetingStore = useMeetingStore()
const userStore = useUserStore()
const {
  state, channelName, meetingTitle, localAudioTrack, localVideoTrack, remoteUsers, userMap,
  localAudioEnabled, localVideoEnabled, errorMessage, leaveMeeting, toggleMute, toggleCamera
} = useLiveKit()

const roomRoot = ref(null)
const localVideoEl = ref(null)
const remoteVideoEls = {}
const duration = ref(0)
let timer = null

const localName = computed(() => userStore.displayName || '我')
const localInitial = computed(() => localName.value[0]?.toUpperCase() || '我')
const localAvatar = computed(() => userStore.userInfo?.avatarUrl || null)
const remoteUserCount = computed(() => Object.keys(remoteUsers).length)
const stateLabel = computed(() => ({ joining: '正在加入', joined: '已连接', reconnecting: '正在重连', leaving: '正在离开' })[state.value] || '准备中')
const gridStyle = computed(() => {
  const count = remoteUserCount.value + 1 + (remoteUserCount.value === 0 ? 1 : 0)
  const columns = count <= 1 ? 1 : count <= 4 ? 2 : count <= 9 ? 3 : 4
  return { gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }
})

function participantName(uid) { return userMap[uid]?.userName || userMap[uid]?.username || '参会人' }
function participantInitial(uid) { return participantName(uid)[0]?.toUpperCase() || '?' }
function bindRemoteVideo(el, uid) {
  if (!el) { delete remoteVideoEls[uid]; return }
  remoteVideoEls[uid] = el
  const track = remoteUsers[uid]?.videoTrack
  const trackId = track?.sid || track?.mediaStreamTrack?.id || ''
  if (track && el.dataset.livekitTrack !== trackId) {
    try {
      track.attach(el)
      el.dataset.livekitTrack = trackId
    } catch {}
  }
}
function attachLocalVideo() {
  nextTick(() => {
    if (!localVideoEl.value || !localVideoTrack.value || !localVideoEnabled.value) return
    const trackId = localVideoTrack.value.sid || localVideoTrack.value.mediaStreamTrack?.id || ''
    if (localVideoEl.value.dataset.livekitTrack === trackId) return
    try {
      localVideoTrack.value.attach(localVideoEl.value)
      localVideoEl.value.dataset.livekitTrack = trackId
    } catch {}
  })
}
watch([localVideoTrack, localVideoEnabled, () => props.visible], attachLocalVideo)
const remoteVideoSignature = computed(() => Object.entries(remoteUsers)
  .map(([uid, remote]) => `${uid}:${remote.videoTrack?.sid || ''}:${remote.hasVideo ? 1 : 0}`)
  .join('|'))
watch(remoteVideoSignature, () => nextTick(() => {
  Object.keys(remoteUsers).forEach(uid => bindRemoteVideo(remoteVideoEls[uid], uid))
}))

watch(() => props.visible, (visible) => {
  if (timer) { clearInterval(timer); timer = null }
  if (visible) { duration.value = 0; timer = setInterval(() => duration.value += 1, 1000); attachLocalVideo() }
}, { immediate: true })

async function handleEnd() {
  if (meetingStore.current?.id) await meetingStore.leave(meetingStore.current.id)
  await leaveMeeting()
  emit('update:visible', false)
  emit('ended')
}
async function toggleFullscreen() {
  try {
    if (document.fullscreenElement) await document.exitFullscreen()
    else await roomRoot.value?.requestFullscreen?.()
  } catch {}
}
function formatDuration(value) {
  const minutes = Math.floor(value / 60).toString().padStart(2, '0')
  const seconds = (value % 60).toString().padStart(2, '0')
  return `${minutes}:${seconds}`
}
onUnmounted(() => { if (timer) clearInterval(timer); if (state.value !== 'idle') leaveMeeting() })
</script>

<style scoped>
.meeting-room { position: fixed; inset: 0; z-index: 1000; display: flex; min-width: 760px; flex-direction: column; overflow: hidden; color: #eef4ff; background: #07101e; }
.meeting-header { position: relative; z-index: 3; display: flex; height: 72px; flex: 0 0 auto; align-items: center; justify-content: space-between; padding: 0 22px; border-bottom: 1px solid rgba(148,163,184,.12); background: rgba(11,20,36,.88); backdrop-filter: blur(18px); }.meeting-title-block,.meeting-header-actions,.meeting-title-line { display: flex; align-items: center; }.meeting-title-block { gap: 12px; min-width: 0; }.header-back,.header-action { display: grid; width: 36px; height: 36px; place-items: center; border: 1px solid rgba(255,255,255,.1); border-radius: 11px; color: #cbd6e9; background: rgba(255,255,255,.055); transition: .18s; }.header-back:hover,.header-action:hover { color: white; border-color: rgba(112,153,255,.35); background: rgba(78,117,222,.18); }.header-back svg,.header-action svg { width: 18px; }.meeting-title-line { gap: 9px; }.meeting-title-line h1 { max-width: 390px; overflow: hidden; font-size: 15px; font-weight: 700; text-overflow: ellipsis; white-space: nowrap; }.meeting-title-block p { margin-top: 3px; color: #71809b; font-size: 10px; }.meeting-title-block p span { margin: 0 4px; }.meeting-live { display: inline-flex; align-items: center; gap: 5px; padding: 3px 7px; border-radius: 99px; color: #7de0c2; background: rgba(33,190,141,.1); font-size: 9px; }.meeting-live i { width: 5px; height: 5px; border-radius: 50%; background: #38d39f; box-shadow: 0 0 0 4px rgba(56,211,159,.1); }.meeting-header-actions { gap: 9px; }.participant-count { display: flex; align-items: center; gap: 6px; height: 34px; padding: 0 10px; border: 1px solid rgba(255,255,255,.1); border-radius: 11px; color: #b9c6db; background: rgba(255,255,255,.045); font-size: 10px; }.participant-count svg { width: 15px; }
.meeting-stage { position: relative; flex: 1; min-height: 0; overflow: auto; padding: 20px 22px 118px; background: radial-gradient(circle at 50% 0%, #13213a 0%, #080f1c 45%, #050a13 100%); }.stage-glow { position: absolute; width: 360px; height: 360px; border-radius: 50%; opacity: .11; filter: blur(80px); pointer-events: none; }.glow-one { top: 3%; left: 7%; background: #3d7cff; }.glow-two { right: 5%; bottom: 3%; background: #2dc9a7; }.media-grid { position: relative; z-index: 1; display: grid; min-height: 100%; gap: 12px; align-content: center; }.participant-tile { position: relative; min-height: 220px; overflow: hidden; border: 1px solid rgba(148,163,184,.15); border-radius: 20px; background: linear-gradient(145deg,#162236,#0d1728); box-shadow: 0 16px 40px rgba(0,0,0,.18); }.participant-tile::after { content: ''; position: absolute; inset: 0; pointer-events: none; box-shadow: inset 0 0 0 1px rgba(255,255,255,.02); border-radius: inherit; }.participant-tile video { width: 100%; height: 100%; object-fit: cover; }.participant-tile.camera-on { border-color: rgba(87,130,245,.3); }.camera-placeholder { position: absolute; inset: 0; display: flex; align-items: center; justify-content: center; flex-direction: column; background: radial-gradient(circle at 50% 34%, rgba(66,102,173,.24), transparent 42%); }.avatar { display: grid; width: 84px; height: 84px; place-items: center; overflow: hidden; border: 4px solid rgba(255,255,255,.09); border-radius: 28px; color: white; background: linear-gradient(145deg,#496ff1,#755fea); box-shadow: 0 14px 36px rgba(0,0,0,.28); font-size: 27px; font-weight: 700; }.avatar-local { background: linear-gradient(145deg,#2778ef,#29b39d); }.avatar img { width: 100%; height: 100%; object-fit: cover; }.camera-placeholder strong { margin-top: 15px; color: #e6edf9; font-size: 12px; }.camera-placeholder small { margin-top: 4px; color: #74829a; font-size: 10px; }.tile-topline { position: absolute; top: 13px; left: 13px; z-index: 2; display: flex; align-items: center; gap: 6px; }.tile-topline span,.tile-topline em { padding: 4px 7px; border-radius: 7px; background: rgba(2,7,15,.56); backdrop-filter: blur(8px); font-size: 9px; font-style: normal; }.tile-topline em { color: #76dbbd; }.tile-info { position: absolute; right: 12px; bottom: 12px; left: 12px; z-index: 2; display: flex; align-items: center; justify-content: space-between; }.tile-name { max-width: 75%; overflow: hidden; padding: 5px 8px; border-radius: 8px; color: white; background: rgba(2,7,15,.62); backdrop-filter: blur(8px); font-size: 10px; font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }.media-off { display: grid; width: 28px; height: 28px; place-items: center; border-radius: 9px; color: #ff96a3; background: rgba(94,21,35,.68); backdrop-filter: blur(8px); }.media-off svg { width: 14px; }.waiting-tile { display: flex; align-items: center; justify-content: center; flex-direction: column; border-style: dashed; background: rgba(13,23,40,.52); }.waiting-icon { display: flex; gap: 5px; margin-bottom: 14px; }.waiting-icon span { width: 7px; height: 7px; border-radius: 50%; background: #547cf2; animation: waiting 1.2s infinite ease-in-out; }.waiting-icon span:nth-child(2) { animation-delay: .16s; }.waiting-icon span:nth-child(3) { animation-delay: .32s; }.waiting-tile strong { font-size: 12px; }.waiting-tile small { margin-top: 5px; color: #71809b; font-size: 10px; }
.meeting-footer { position: absolute; right: 0; bottom: 0; left: 0; z-index: 4; display: flex; height: 106px; align-items: center; justify-content: center; pointer-events: none; background: linear-gradient(transparent,rgba(4,9,17,.92)); }.control-dock { display: flex; align-items: center; gap: 8px; padding: 9px 12px; border: 1px solid rgba(255,255,255,.11); border-radius: 22px; background: rgba(18,29,48,.9); box-shadow: 0 20px 55px rgba(0,0,0,.4); backdrop-filter: blur(20px); pointer-events: auto; }.control-button { display: flex; min-width: 78px; align-items: center; justify-content: center; flex-direction: column; gap: 5px; color: #c7d2e5; }.control-button span { display: grid; width: 42px; height: 42px; place-items: center; border: 1px solid rgba(255,255,255,.1); border-radius: 14px; background: rgba(255,255,255,.07); transition: .18s; }.control-button:hover span { color: white; background: rgba(255,255,255,.13); transform: translateY(-2px); }.control-button svg { width: 19px; }.control-button em { font-size: 9px; font-style: normal; }.control-button.off span { color: #ff98a6; border-color: rgba(245,86,106,.18); background: rgba(207,52,72,.18); }.control-button.hangup span { color: white; border-color: transparent; background: linear-gradient(145deg,#f05b68,#d9364d); box-shadow: 0 9px 24px rgba(217,54,77,.28); }.control-button.hangup { min-width: 88px; }.dock-separator { width: 1px; height: 38px; margin: 0 5px; background: rgba(255,255,255,.1); }
.meeting-error { position: absolute; top: 84px; left: 50%; z-index: 10; display: flex; width: min(500px,calc(100vw - 48px)); align-items: center; gap: 10px; padding: 11px 13px; border: 1px solid rgba(255,105,124,.28); border-radius: 14px; background: rgba(78,19,31,.92); box-shadow: 0 15px 45px rgba(0,0,0,.25); transform: translateX(-50%); backdrop-filter: blur(12px); }.meeting-error > span { display: grid; width: 27px; height: 27px; flex: 0 0 27px; place-items: center; border-radius: 9px; color: white; background: #e34b61; font-weight: 800; }.meeting-error div { min-width: 0; flex: 1; }.meeting-error strong { font-size: 10px; }.meeting-error p { margin-top: 2px; overflow: hidden; color: #f4b9c1; font-size: 9px; text-overflow: ellipsis; white-space: nowrap; }.meeting-error button { color: #dba8b0; font-size: 20px; }.meeting-fade-enter-active,.meeting-fade-leave-active { transition: opacity .24s ease; }.meeting-fade-enter-from,.meeting-fade-leave-to { opacity: 0; }.error-slide-enter-active,.error-slide-leave-active { transition: .2s ease; }.error-slide-enter-from,.error-slide-leave-to { opacity: 0; transform: translate(-50%,-10px); }video[data-local-video] { transform: scaleX(-1); }
@keyframes waiting { 0%,80%,100% { opacity: .3; transform: translateY(0); } 40% { opacity: 1; transform: translateY(-4px); } }
@media (max-width: 980px) { .meeting-header { padding: 0 14px; }.meeting-title-line h1 { max-width: 220px; }.participant-count { display: none; }.meeting-stage { padding: 14px 14px 112px; }.participant-tile { min-height: 180px; border-radius: 16px; } }
@media (max-width: 720px) { .meeting-room { min-width: 0; }.meeting-header { height: 64px; }.meeting-title-block { gap: 8px; }.meeting-title-line h1 { max-width: 48vw; font-size: 13px; }.meeting-title-block p { max-width: 58vw; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }.meeting-live,.meeting-header-actions :deep(.performance-indicator) { display: none; }.media-grid { grid-template-columns: 1fr !important; align-content: start; }.participant-tile { min-height: min(52vw,260px); }.meeting-stage { padding: 10px 10px 104px; }.control-dock { bottom: 12px; gap: 5px; padding: 7px 8px; }.control-button { min-width: 64px; }.control-button span { width: 40px; height: 40px; }.control-button em { font-size: 9px; } }
</style>
