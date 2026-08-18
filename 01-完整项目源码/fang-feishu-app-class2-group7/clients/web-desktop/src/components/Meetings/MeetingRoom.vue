<template>
  <Teleport to="body">
    <transition name="fade" appear>
      <div v-if="visible" class="fixed inset-0 z-[60] bg-gray-900 flex flex-col">
        <!-- 头部 -->
        <div class="h-12 px-5 flex items-center justify-between bg-gray-800/50 text-white">
          <div class="flex items-center space-x-3">
            <span class="text-sm">{{ meetingTitle || '会议中' }}</span>
            <span class="text-xs text-gray-400 font-mono">{{ formatDuration(duration) }}</span>
          </div>
          <div class="text-xs text-gray-400">
            {{ stateLabel }} · {{ remoteUserCount + 1 }} 人
          </div>
        </div>

        <!-- 媒体 grid -->
        <div class="flex-1 bg-black p-4 overflow-auto">
          <div class="h-full" :style="gridStyle" :class="gridClass">
            <!-- 本地 tile -->
            <div class="relative bg-gray-800 rounded-lg overflow-hidden border border-gray-700 aspect-video flex items-center justify-center">
              <video v-if="localVideoTrack && localVideoEnabled"
                     data-local-video
                     ref="localVideoEl"
                     autoplay playsinline muted
                     class="w-full h-full object-cover"></video>
              <div v-else class="flex flex-col items-center text-white">
                <div class="w-20 h-20 rounded-full bg-primary flex items-center justify-center text-3xl">
                  我
                </div>
                <p class="mt-3 text-sm">{{ localVideoEnabled ? '本地预览' : '摄像头已关' }}</p>
              </div>
              <div class="absolute bottom-2 left-2 px-2 py-0.5 bg-black/60 rounded text-xs text-white flex items-center gap-1">
                <span>我</span>
                <svg v-if="!localAudioEnabled" class="w-3 h-3 text-red-400" fill="currentColor" viewBox="0 0 20 20" title="已静音"><path d="M3.707 2.293a1 1 0 00-1.414 1.414l14 14a1 1 0 001.414-1.414l-1.473-1.473A8.938 8.938 0 0021 10V8a1 1 0 10-2 0v2a6.95 6.95 0 01-1.594 4.5L15 8.83V4a3 3 0 00-5.236-2.135L8.13 3.547 3.707 2.293zM10 14a2 2 0 11-4 0 2 2 0 014 0z"/></svg>
                <svg v-if="!localVideoEnabled" class="w-3 h-3 text-red-400" fill="currentColor" viewBox="0 0 20 20" title="摄像头已关"><path d="M2 6a2 2 0 012-2h6a2 2 0 012 2v8a2 2 0 01-2 2H4a2 2 0 01-2-2V6zM14.553 7.106A1 1 0 0114 8v4a1 1 0 01-.553.894l-2 1A1 1 0 0110 13V7a1 1 0 011.447-.894l2 1z"/></svg>
              </div>
            </div>

            <!-- 远端 tiles -->
            <div v-for="(r, uid) in remoteUsers" :key="`remote-${uid}`"
                 class="relative bg-gray-800 rounded-lg overflow-hidden border border-gray-700 aspect-video flex items-center justify-center">
              <video v-if="r.videoTrack && r.hasVideo"
                     :ref="(el) => bindRemoteVideo(el, uid)"
                     autoplay playsinline
                     class="w-full h-full object-cover"></video>
              <div v-else class="flex flex-col items-center text-white">
                <div class="w-20 h-20 rounded-full bg-primary flex items-center justify-center text-2xl font-medium shadow-md">
                  {{ (userMap[uid]?.userName?.[0]) || (userMap[uid]?.username?.[0]) || '?' }}
                </div>
                <p class="mt-3 text-sm font-medium">{{ userMap[uid]?.userName || userMap[uid]?.username || '远端' }}</p>
                <p v-if="r.hasAudio && !r.hasVideo" class="text-xs text-gray-400">仅音频</p>
              </div>
              <div class="absolute bottom-2 left-2 px-2 py-0.5 bg-black/60 rounded text-xs text-white flex items-center gap-1">
                <span class="truncate max-w-[100px]">{{ userMap[uid]?.userName || userMap[uid]?.username || `uid ${uid}` }}</span>
                <svg v-if="!r.hasAudio" class="w-3 h-3 text-red-400" fill="currentColor" viewBox="0 0 20 20" title="已静音"><path d="M3.707 2.293a1 1 0 00-1.414 1.414l14 14a1 1 0 001.414-1.414l-1.473-1.473A8.938 8.938 0 0021 10V8a1 1 0 10-2 0v2a6.95 6.95 0 01-1.594 4.5L15 8.83V4a3 3 0 00-5.236-2.135L8.13 3.547 3.707 2.293zM10 14a2 2 0 11-4 0 2 2 0 014 0z"/></svg>
                <svg v-if="!r.hasVideo" class="w-3 h-3 text-red-400" fill="currentColor" viewBox="0 0 20 20" title="摄像头已关"><path d="M2 6a2 2 0 012-2h6a2 2 0 012 2v8a2 2 0 01-2 2H4a2 2 0 01-2-2V6zM14.553 7.106A1 1 0 0114 8v4a1 1 0 01-.553.894l-2 1A1 1 0 0110 13V7a1 1 0 011.447-.894l2 1z"/></svg>
              </div>
            </div>
          </div>
        </div>

        <!-- 错误提示 -->
        <div v-if="errorMessage" class="absolute top-16 left-1/2 -translate-x-1/2 bg-red-500/90 text-white px-4 py-2 rounded text-sm shadow-lg">
          ⚠️ {{ errorMessage }}
          <button @click="errorMessage = ''" class="ml-2 text-white/80 hover:text-white">✕</button>
        </div>

        <!-- 控制栏 -->
        <div class="h-20 bg-gray-800/80 flex items-center justify-center space-x-6">
          <button @click="toggleMute" :class="controlBtnClass(!localAudioEnabled)"
                  :title="localAudioEnabled ? '静音' : '取消静音'">
            <svg v-if="localAudioEnabled" class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path d="M7 4a3 3 0 016 0v6a3 3 0 11-6 0V4zM2 9a1 1 0 112 0 5 5 0 0010 0 1 1 0 112 0 7 7 0 01-6 6.93V17h2a1 1 0 110 2H8a1 1 0 110-2h2v-1.07A7 7 0 012 9z" />
            </svg>
            <svg v-else class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path d="M3.707 2.293a1 1 0 00-1.414 1.414l14 14a1 1 0 001.414-1.414l-1.473-1.473A8.938 8.938 0 0021 10V8a1 1 0 10-2 0v2a6.95 6.95 0 01-1.594 4.5L15 8.83V4a3 3 0 00-5.236-2.135L8.13 3.547 3.707 2.293zM10 14a2 2 0 11-4 0 2 2 0 014 0z" />
            </svg>
          </button>

          <button @click="toggleCamera" :class="controlBtnClass(!localVideoEnabled)"
                  :title="localVideoEnabled ? '关闭摄像头' : '开启摄像头'">
            <svg v-if="localVideoEnabled" class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path d="M2 6a2 2 0 012-2h6a2 2 0 012 2v8a2 2 0 01-2 2H4a2 2 0 01-2-2V6zM14.553 7.106A1 1 0 0114 8v4a1 1 0 01-.553.894l-2 1A1 1 0 0110 13V7a1 1 0 011.447-.894l2 1z" />
            </svg>
            <svg v-else class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M2.293 2.293a1 1 0 011.414 0l14 14a1 1 0 01-1.414 1.414l-2.275-2.275A2 2 0 0112 16H4a2 2 0 01-2-2V6c0-.74.402-1.386 1-1.732l-.707-.561a1 1 0 010-1.414zM16 7.382l1.553-.776A1 1 0 0119 7.5v5a1 1 0 01-1.447.894L16 12.618V7.382zM4.805 4l8 8V6a2 2 0 00-2-2H4.805z" clip-rule="evenodd" />
            </svg>
          </button>

          <button @click="handleEnd" class="w-14 h-14 rounded-full bg-red-500 hover:bg-red-600 flex items-center justify-center text-white shadow-lg"
                  title="结束会议">
            <svg class="w-6 h-6 rotate-135" fill="currentColor" viewBox="0 0 20 20">
              <path d="M2 3a1 1 0 011-1h2.153a1 1 0 01.986.836l.74 4.435a1 1 0 01-.54 1.06l-1.746.872a.5.5 0 00-.215.555l.972 2.917a5.5 5.5 0 005.732 4.066l1.745-.872a1 1 0 011.06-.54l4.435.74a1 1 0 01.836.986V17a1 1 0 01-1 1h-2C7.82 18 2 12.18 2 5V3z" />
            </svg>
          </button>
        </div>
      </div>
    </transition>
  </Teleport>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue'
import { useAgora } from '@/composables/useAgora'
import { useMeetingStore } from '@/stores/meeting'

const props = defineProps({ visible: Boolean })
const emit = defineEmits(['update:visible', 'ended'])

const {
  state, meetingTitle, localAudioTrack, localVideoTrack, remoteUsers, userMap,
  localAudioEnabled, localVideoEnabled, errorMessage,
  joinMeeting, leaveMeeting, toggleMute, toggleCamera
} = useAgora()
const meetingStore = useMeetingStore()

const localVideoEl = ref(null)
const remoteVideoRefs = {}  // uid -> el

const stateLabel = computed(() => ({
  joining: '正在加入',
  joined: '已连接',
  leaving: '已结束'
}[state.value] || ''))

const remoteUserCount = computed(() => Object.keys(remoteUsers).length)

function colCount(n) {
  if (n <= 1) return 1
  if (n <= 4) return 2
  if (n <= 9) return 3
  return 4
}

const gridClass = 'grid gap-2 w-full h-full'
const gridStyle = computed(() => ({
  gridTemplateColumns: `repeat(${colCount(remoteUserCount.value + 1)}, minmax(0, 1fr))`,
  gridAutoRows: 'minmax(120px, 1fr)'
}))

/** 远端 video 元素绑定 */
function bindRemoteVideo(el, uid) {
  if (el) {
    remoteVideoRefs[uid] = el
    const r = remoteUsers[uid]
    if (r?.videoTrack) {
      el.srcObject = new MediaStream([r.videoTrack.getMediaStreamTrack?.()].filter(Boolean))
    }
  }
}

/** 监听远端 videoTrack 变化，更新 srcObject */
watch(remoteUsers, (val) => {
  nextTick(() => {
    for (const uid in val) {
      const r = val[uid]
      const el = remoteVideoRefs[uid]
      if (el && r?.videoTrack && !el.srcObject) {
        const track = r.videoTrack.getMediaStreamTrack?.()
        if (track) el.srcObject = new MediaStream([track])
      }
    }
  })
}, { deep: true })

/** 监听本地 videoTrack：useAgora 已通过 track.play() 绑定到 [data-local-video] 元素，
 *  这里只做兜底（如果 track.play 不可用，再走 srcObject） */
watch([localVideoTrack, () => props.visible, localVideoEnabled], () => {
  nextTick(() => {
    if (localVideoEl.value && localVideoTrack.value && !localVideoEl.value.srcObject) {
      const track = localVideoTrack.value.getMediaStreamTrack?.()
      if (track) {
        localVideoEl.value.srcObject = new MediaStream([track])
      }
    }
  })
}, { immediate: false })

async function handleEnd() {
  if (meetingStore.current) {
    await meetingStore.leave(meetingStore.current.id)
  }
  await leaveMeeting()
  emit('ended')
}

const duration = ref(0)
let timer = null

watch(() => props.visible, (v) => {
  if (v) {
    duration.value = 0
    timer = setInterval(() => duration.value++, 1000)
  } else {
    if (timer) { clearInterval(timer); timer = null }
  }
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
  if (state.value !== 'idle') leaveMeeting()
})

function formatDuration(s) {
  const m = Math.floor(s / 60).toString().padStart(2, '0')
  const sec = (s % 60).toString().padStart(2, '0')
  return `${m}:${sec}`
}

function controlBtnClass(inactive) {
  return [
    'w-12 h-12 rounded-full flex items-center justify-center transition',
    inactive ? 'bg-red-500 text-white' : 'bg-gray-700 text-white hover:bg-gray-600'
  ]
}
</script>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity 0.2s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

/* 本地视频不镜像（用户自然视角）
   远端视频不镜像（保持原始方向） */
video[data-local-video] { transform: none !important; }
video:not([data-local-video]) { transform: none !important; }
</style>