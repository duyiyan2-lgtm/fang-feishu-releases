<template>
  <Teleport to="body">
    <transition name="dialog" appear>
      <div v-if="modelValue" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
           @click.self="onClose">
        <div class="bg-white dark:bg-gray-900 rounded-xl shadow-2xl w-[560px] max-h-[80vh] flex flex-col">
          <!-- Header -->
          <div class="px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
            <div>
              <h3 class="text-base font-medium dark:text-gray-100">会议中心</h3>
              <p class="text-xs text-gray-500 mt-0.5">选择已有会议进入，或创建新会议</p>
            </div>
            <button @click="onClose" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 text-lg">✕</button>
          </div>

          <!-- 标签切换 -->
          <div class="px-6 pt-3 flex items-center gap-1 text-sm">
            <button v-for="t in tabs" :key="t.value" @click="activeTab = t.value"
                    :class="['px-3 h-8 rounded transition',
                             activeTab === t.value
                               ? 'bg-primary text-white'
                               : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800']">
              {{ t.label }}
              <span v-if="t.count !== null" class="ml-1 text-xs opacity-80">({{ t.count }})</span>
            </button>
          </div>

          <!-- 会议列表 -->
          <div class="flex-1 overflow-y-auto px-6 py-3 min-h-[260px]">
            <div v-if="loading" class="text-center py-8 text-sm text-gray-400">加载中…</div>
            <div v-else-if="filteredMeetings.length === 0" class="text-center py-12">
              <VideoCameraIcon class="w-10 h-10 mx-auto text-gray-300 dark:text-gray-700 mb-2" />
              <p class="text-sm text-gray-500">
                {{ activeTab === 'active' ? '当前没有进行中的会议' :
                   activeTab === 'mine' ? '你还没创建过会议' : '没有更多历史会议' }}
              </p>
            </div>
            <div v-else class="space-y-2">
              <div v-for="m in filteredMeetings" :key="m.id"
                   class="border border-gray-200 dark:border-gray-700 rounded-lg p-3 hover:border-primary/40 transition">
                <div class="flex items-start justify-between gap-3">
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2">
                      <h4 class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
                        {{ m.title || '未命名会议' }}
                      </h4>
                      <span :class="['px-1.5 py-0.5 rounded text-[10px] flex-shrink-0', statusClass(m.status)]">
                        {{ statusLabel(m.status) }}
                      </span>
                      <span v-if="m.createdBy === meId" class="px-1.5 py-0.5 rounded text-[10px] bg-amber-50 text-amber-600 flex-shrink-0">我创建</span>
                    </div>
                    <div class="mt-1 text-xs text-gray-500 dark:text-gray-400 flex items-center gap-3">
                      <span>📍 {{ m.roomId }}</span>
                      <span>👥 {{ (m.members || []).length }} 人</span>
                      <span>🕒 {{ formatTime(m.createdAt) }}</span>
                    </div>
                  </div>
                  <div class="flex flex-col gap-1.5 flex-shrink-0">
                    <button v-if="canJoin(m)" @click.stop="onJoin(m)" :disabled="busyId === m.id"
                            class="h-7 px-3 text-xs bg-primary text-white rounded hover:bg-primary-hover disabled:opacity-50">
                      {{ busyId === m.id ? '加入中…' : '加入' }}
                    </button>
                    <button v-if="m.createdBy === meId && m.status === 'Active'" @click.stop="onEnd(m)" :disabled="busyId === m.id"
                            class="h-7 px-3 text-xs border border-red-200 text-red-600 dark:border-red-800 dark:text-red-400 rounded hover:bg-red-50 dark:hover:bg-red-900/20 disabled:opacity-50">
                      结束
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- 错误提示 -->
          <div v-if="errorMessage" class="mx-6 mb-2 bg-red-500/10 text-red-600 dark:text-red-400 px-3 py-2 rounded text-xs">
            ⚠️ {{ errorMessage }}
            <button @click="errorMessage = ''" class="ml-2">✕</button>
          </div>

          <!-- Footer：创建新会议 -->
          <div class="px-6 py-3 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between">
            <span class="text-xs text-gray-500">后端分配声网频道，自动加入房间</span>
            <button @click="onCreateNew" class="h-8 px-4 text-sm bg-primary text-white rounded hover:bg-primary-hover flex items-center">
              <PlusIcon class="w-4 h-4 mr-1" />创建新会议
            </button>
          </div>
        </div>
      </div>
    </transition>
  </Teleport>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useMeetingStore } from '@/stores/meeting'
import { useUserStore } from '@/stores/user'
import { ElMessage } from '@/api/toast'
import { VideoCameraIcon, PlusIcon } from '@heroicons/vue/24/outline'

const props = defineProps({ modelValue: Boolean })
const emit = defineEmits(['update:modelValue', 'join', 'createNew'])

const meetingStore = useMeetingStore()
const userStore = useUserStore()
const meId = computed(() => userStore.userInfo?.id)

const loading = ref(false)
const activeTab = ref('active')
const busyId = ref(null)
const errorMessage = ref('')

const tabs = computed(() => {
  const all = meetingStore.list || []
  return [
    { label: '进行中', value: 'active', count: all.filter(m => m.status === 'Active').length },
    { label: '我创建的', value: 'mine', count: all.filter(m => m.createdBy === meId.value).length },
    { label: '全部', value: 'all', count: all.length }
  ]
})

const filteredMeetings = computed(() => {
  const all = meetingStore.list || []
  // 按创建时间倒序
  const sorted = [...all].sort((a, b) => {
    const ta = new Date(a.createdAt || 0).getTime()
    const tb = new Date(b.createdAt || 0).getTime()
    return tb - ta
  })
  if (activeTab.value === 'active') return sorted.filter(m => m.status === 'Active')
  if (activeTab.value === 'mine') return sorted.filter(m => m.createdBy === meId.value)
  return sorted
})

function canJoin(m) {
  if (m.status !== 'Active') return false
  // 我创建的 / 我是成员的 / 我被邀请的 都能 join
  if (m.createdBy === meId.value) return true
  if ((m.members || []).some(x => x.userId === meId.value)) return true
  // 后端返回里没 invited 标记；只要我能在 GET /meetings 里看到，就说明我有访问权
  return true
}

function statusClass(s) {
  if (s === 'Active') return 'bg-green-50 text-green-600 dark:bg-green-500/20 dark:text-green-300'
  if (s === 'Ended') return 'bg-gray-100 text-gray-500'
  return 'bg-blue-50 text-blue-600'
}
function statusLabel(s) { return { Active: '进行中', Ended: '已结束' }[s] || s }
function formatTime(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  const now = Date.now()
  const diff = (now - d.getTime()) / 1000
  if (diff < 60) return '刚刚'
  if (diff < 3600) return `${Math.floor(diff / 60)} 分钟前`
  if (diff < 86400) return `${Math.floor(diff / 3600)} 小时前`
  return d.toISOString().slice(0, 10)
}

async function loadList() {
  loading.value = true
  try {
    await meetingStore.fetchList()
  } catch (e) {
    errorMessage.value = '加载会议列表失败：' + (e?.message || '')
  } finally {
    loading.value = false
  }
}

async function onJoin(m) {
  busyId.value = m.id
  errorMessage.value = ''
  try {
    const data = await meetingStore.join(m.id)
    if (!data) throw new Error('加入返回为空')
    emit('join', m, data)
    onClose()
  } catch (e) {
    errorMessage.value = '加入失败：' + (e?.message || '需要被邀请')
  } finally {
    busyId.value = null
  }
}

async function onEnd(m) {
  if (!confirm(`确定结束会议「${m.title || m.roomId}」？`)) return
  busyId.value = m.id
  try {
    await meetingStore.end(m.id)
    ElMessage({ message: '会议已结束', type: 'success' })
    await loadList()
  } catch (e) {
    ElMessage({ message: '结束失败：' + (e?.message || ''), type: 'error' })
  } finally {
    busyId.value = null
  }
}

function onCreateNew() {
  emit('createNew')
  onClose()
}

function onClose() {
  emit('update:modelValue', false)
}

watch(() => props.modelValue, (v) => {
  if (v) {
    activeTab.value = 'active'
    loadList()
  }
})

onMounted(() => { if (props.modelValue) loadList() })
</script>

<style scoped>
.dialog-enter-active, .dialog-leave-active { transition: opacity 0.2s; }
.dialog-enter-from, .dialog-leave-to { opacity: 0; }
</style>
