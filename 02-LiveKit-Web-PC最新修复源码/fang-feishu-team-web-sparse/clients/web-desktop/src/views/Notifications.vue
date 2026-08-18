<template>
  <div class="flex flex-col h-full bg-white dark:bg-gray-900 transition-colors">
    <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700 flex-shrink-0">
      <div class="flex items-center space-x-3">
        <h1 class="text-base font-medium text-gray-900 dark:text-gray-100">消息通知</h1>
        <span v-if="notifStore.unreadCount > 0" class="px-2 py-0.5 rounded-full text-xs bg-red-500/10 text-red-500 font-medium">
          {{ notifStore.unreadCount }} 条未读
        </span>
        <span v-else class="px-2 py-0.5 rounded-full text-xs bg-green-500/10 text-green-600 font-medium">全部已读</span>
      </div>
      <div class="flex items-center space-x-2">
        <button @click="markAllRead" :disabled="notifStore.unreadCount === 0" class="h-8 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md hover:bg-gray-50 dark:hover:bg-gray-800 dark:text-gray-200 transition disabled:opacity-50">
          全部标为已读
        </button>
        <button class="h-8 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md hover:bg-gray-50 dark:hover:bg-gray-800 dark:text-gray-200 transition">
          ⚙️ 通知设置
        </button>
      </div>
    </div>

    <div class="px-6 py-3 border-b border-gray-100 dark:border-gray-800 flex items-center space-x-1 flex-shrink-0">
      <button v-for="t in tabs" :key="t.value" @click="activeTab = t.value"
              :class="['px-3 h-8 text-sm rounded transition',
                       activeTab === t.value
                         ? 'bg-primary text-white'
                         : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300']">
        {{ t.label }}
        <span v-if="t.count" class="ml-1 text-xs" :class="activeTab === t.value ? 'opacity-90' : 'opacity-70'">({{ t.count }})</span>
      </button>
    </div>

    <div class="flex-1 overflow-y-auto">
      <div class="max-w-3xl mx-auto px-6 py-4">
        <div v-if="filteredList.length === 0" class="text-center py-20">
          <BellSlashIcon class="w-12 h-12 mx-auto text-gray-300 dark:text-gray-700 mb-3" />
          <p class="text-sm text-gray-500">暂无通知</p>
        </div>

        <transition-group name="notif" tag="div" class="space-y-2">
          <div v-for="n in filteredList" :key="n.id"
               @click="onNotifClick(n)"
               class="bg-white dark:bg-gray-800 border rounded-lg p-4 cursor-pointer transition-all hover:shadow-md"
               :class="n.read
                 ? 'border-gray-200 dark:border-gray-700'
                 : 'border-primary/30 bg-primary-50/30 dark:bg-primary/5'">
            <div class="flex">
              <div class="flex-shrink-0 w-10 h-10 rounded-lg flex items-center justify-center text-white"
                   :style="{ background: n.color }">
                <component :is="iconFor(n.type)" class="w-5 h-5" />
              </div>
              <div class="ml-4 flex-1 min-w-0">
                <div class="flex items-center justify-between">
                  <div class="flex items-center">
                    <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100"
                        :class="!n.read ? 'font-semibold' : ''">{{ n.title }}</h3>
                    <span v-if="!n.read" class="ml-2 w-2 h-2 rounded-full bg-red-500"></span>
                  </div>
                  <span class="text-xs text-gray-400 ml-2">{{ n.createdAt }}</span>
                </div>
                <p class="mt-1 text-sm text-gray-600 dark:text-gray-300">{{ n.content }}</p>
                <div class="mt-2 flex items-center text-xs text-gray-400">
                  <span>来自 {{ n.source }}</span>
                  <span class="mx-2">·</span>
                  <span :class="typeColor(n.type)">{{ typeLabel(n.type) }}</span>
                  <span v-if="n.resourceType" class="mx-2">·</span>
                  <span class="text-primary opacity-70">点击查看 →</span>
                </div>
              </div>
            </div>
          </div>
        </transition-group>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useNotificationsStore } from '@/stores/notifications'
import { ElMessage } from '@/api/toast'
import {
  BellSlashIcon, AtSymbolIcon, MegaphoneIcon, ClipboardDocumentCheckIcon,
  ChatBubbleLeftRightIcon, HandThumbUpIcon, VideoCameraIcon, DocumentIcon, FolderIcon
} from '@heroicons/vue/24/outline'

const notifStore = useNotificationsStore()
const router = useRouter()
const activeTab = ref('all')

// 首次进入页面从后端拉真实通知
onMounted(async () => {
  await notifStore.load().catch(() => undefined)
  await notifStore.refreshUnread()
})

// tab 类型对照（按后端真实 type，store 里已 toLowerCase）
const tabs = computed(() => [
  { label: '全部',     value: 'all',      count: notifStore.items.length },
  { label: '消息',     value: 'im',        count: notifStore.items.filter(n => n.type === 'im').length },
  { label: '会议',     value: 'meeting',   count: notifStore.items.filter(n => n.type === 'meeting').length },
  { label: '审批',     value: 'approval',  count: notifStore.items.filter(n => n.type === 'approval').length },
  { label: '文件',     value: 'file',      count: notifStore.items.filter(n => n.type === 'file').length },
  { label: '系统',     value: 'system',    count: notifStore.items.filter(n => n.type === 'system').length }
])

const filteredList = computed(() =>
  activeTab.value === 'all'
    ? notifStore.items
    : notifStore.items.filter(n => (n.type || '').toLowerCase() === (activeTab.value || '').toLowerCase())
)

function iconFor(type) {
  const t = (type || '').toLowerCase()
  return {
    im: ChatBubbleLeftRightIcon, meeting: VideoCameraIcon, approval: ClipboardDocumentCheckIcon,
    file: DocumentIcon, system: MegaphoneIcon, mention: AtSymbolIcon
  }[t] || MegaphoneIcon
}
function typeLabel(type) {
  const t = (type || '').toLowerCase()
  return { im: '消息', meeting: '会议', approval: '审批', file: '文件', system: '系统', mention: '@提及' }[t] || type
}
function typeColor(type) {
  const t = (type || '').toLowerCase()
  return { im: 'text-primary', meeting: 'text-cyan-500', approval: 'text-amber-500', file: 'text-green-500', system: 'text-pink-500', mention: 'text-primary' }[t] || 'text-gray-500'
}

// 点击通知：标已读 + 按资源类型跳转
async function onNotifClick(n) {
  // 等待服务端确认已读，再进行页面跳转，避免组件卸载时请求状态不同步。
  if (!n.read && !(await markRead(n.id))) return
  const rt = (n.resourceType || '').toLowerCase()
  const id = n.resourceId
  if (!rt || !id) return
  const routes = {
    conversation: () => router.push(`/messages?conv=${id}`),
    message: () => router.push(`/messages?conv=${id}`),
    document: () => router.push(`/documents/${id}`),
    approval: () => router.push(`/approvals/${id}`),
    meeting: () => router.push('/messages'),
    file: () => router.push('/cloud'),
  }
  const go = routes[rt]
  if (go) go()
}

async function markRead(id) {
  try {
    await notifStore.markRead(id)
    return true
  } catch (e) {
    ElMessage({ message: '标记已读失败：' + (e?.message || '请稍后重试'), type: 'error' })
    return false
  }
}

async function markAllRead() {
  try {
    await notifStore.markAllRead()
    ElMessage({ message: '已全部标记为已读', type: 'success' })
  } catch (e) {
    ElMessage({ message: '全部标记失败：' + (e?.message || '请稍后重试'), type: 'error' })
  }
}
</script>

<style scoped>
.notif-enter-active, .notif-leave-active { transition: all 0.2s ease; }
.notif-enter-from { opacity: 0; transform: translateY(-10px); }
.notif-leave-to { opacity: 0; }
</style>
