<template>
  <div
    v-if="isElectron"
    class="electron-drag h-9 flex items-center justify-between px-3 flex-shrink-0
           bg-[#0F1115] text-white/90 border-b border-white/5 select-none z-[100]"
  >
    <div class="flex items-center gap-2 min-w-0">
      <img
        src="/icon.png"
        alt=""
        class="w-5 h-5 rounded flex-shrink-0 object-cover"
        @error="($event) => ($event.target.style.display = 'none')"
      />
      <span class="text-xs font-medium tracking-wide truncate">仿飞书工作台</span>
      <span class="text-[10px] text-white/40 hidden sm:inline">PC</span>
    </div>

    <div class="electron-no-drag flex items-center gap-0.5">
      <button
        class="w-10 h-7 rounded hover:bg-white/10 flex items-center justify-center transition-colors"
        title="最小化"
        @click="winControl('minimize')"
      >
        <svg class="w-3.5 h-3.5" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="1.4">
          <path d="M2 6h8" stroke-linecap="round"/>
        </svg>
      </button>
      <button
        class="w-10 h-7 rounded hover:bg-white/10 flex items-center justify-center transition-colors"
        :title="isMaximized ? '还原' : '最大化'"
        @click="winControl('maximize')"
      >
        <svg v-if="!isMaximized" class="w-3 h-3" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="1.4">
          <rect x="2" y="2" width="8" height="8" rx="1"/>
        </svg>
        <svg v-else class="w-3.5 h-3.5" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="1.3">
          <path d="M3.5 4.5h5v5h-5zM3.5 4.5V3.2A1.2 1.2 0 014.7 2h5.1A1.2 1.2 0 0111 3.2v5.1A1.2 1.2 0 019.8 9.5H8.5"/>
        </svg>
      </button>
      <button
        class="w-10 h-7 rounded hover:bg-red-500/90 flex items-center justify-center transition-colors"
        title="关闭"
        @click="winControl('close')"
      >
        <svg class="w-3 h-3" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="1.5">
          <path d="M3 3l6 6M9 3l-6 6" stroke-linecap="round"/>
        </svg>
      </button>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'

const isElectron = ref(false)
const isMaximized = ref(false)

function detectElectron() {
  return !!(window.electronAPI?.isElectron)
}

async function refreshMaximized() {
  try {
    if (window.electronAPI?.isMaximized) {
      isMaximized.value = await window.electronAPI.isMaximized()
    }
  } catch {
    /* ignore */
  }
}

function winControl(action) {
  window.electronAPI?.windowControl?.(action)
  // 最大化状态异步刷新
  if (action === 'maximize') {
    setTimeout(refreshMaximized, 80)
  }
}

let unsub = null

onMounted(() => {
  isElectron.value = detectElectron()
  if (!isElectron.value) return
  document.documentElement.classList.add('is-electron')
  refreshMaximized()
  unsub = window.electronAPI?.onMaximizeChange?.((v) => {
    isMaximized.value = !!v
  })
})

onUnmounted(() => {
  if (typeof unsub === 'function') unsub()
})
</script>
