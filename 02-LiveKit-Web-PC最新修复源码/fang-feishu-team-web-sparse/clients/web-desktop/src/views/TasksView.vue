<template>
  <div class="flex flex-col h-full bg-white dark:bg-gray-900 transition-colors">
    <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700 flex-shrink-0">
      <div class="flex items-center space-x-1">
        <button v-for="t in tabs" :key="t.value" @click="activeTab = t.value"
                :class="['px-3 h-8 text-sm rounded transition-colors',
                         activeTab === t.value
                           ? 'bg-primary text-white'
                           : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300']">
          {{ t.label }}
          <span v-if="t.count" class="ml-1 text-xs opacity-80">({{ t.count }})</span>
        </button>
      </div>
      <div class="flex items-center space-x-2">
        <input v-model="newTitle" placeholder="新建任务，回车提交" @keydown.enter="quickAdd"
               class="h-8 px-3 text-sm bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-md outline-none focus:ring-2 focus:ring-primary/30 w-64 dark:text-gray-100" />
        <button @click="quickAdd" :disabled="!newTitle.trim()"
                class="h-8 px-3 text-sm bg-primary hover:bg-primary-hover text-white rounded-md flex items-center transition disabled:opacity-50">
          <PlusIcon class="w-4 h-4 mr-1" />添加
        </button>
      </div>
    </div>

    <div class="flex-1 overflow-y-auto">
      <div v-if="loading" class="text-center py-12 text-sm text-gray-400">加载中…</div>
      <div v-else-if="filteredTasks.length === 0" class="text-center py-20">
        <ClipboardDocumentListIcon class="w-12 h-12 mx-auto text-gray-300 dark:text-gray-700 mb-3" />
        <p class="text-sm text-gray-500">暂无任务</p>
      </div>
      <div v-else class="max-w-4xl mx-auto px-6 py-4 space-y-2">
        <div v-for="t in filteredTasks" :key="t.id"
             class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4 flex items-start gap-3 hover:shadow-md transition-all"
             :class="{ 'opacity-60': t.status === 'completed' }">
          <input type="checkbox" :checked="t.status === 'completed'" @change="toggle(t)"
                 class="mt-1 w-4 h-4 rounded text-primary" :disabled="busyId === t.id" />
          <div class="flex-1 min-w-0">
            <div class="flex items-center justify-between">
              <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100"
                  :class="t.status === 'completed' ? 'line-through' : ''">{{ t.title }}</h3>
              <div class="flex items-center gap-2">
                <span :class="['px-2 py-0.5 rounded text-xs', priorityClass(t.priority)]">{{ priorityLabel(t.priority) }}</span>
                <span :class="['px-2 py-0.5 rounded text-xs', statusClass(t.status)]">{{ statusLabel(t.status) }}</span>
              </div>
            </div>
            <p v-if="t.description" class="mt-1 text-sm text-gray-600 dark:text-gray-300 line-clamp-2">{{ t.description }}</p>
            <div class="mt-2 text-xs text-gray-400 flex items-center gap-3">
              <span v-if="t.assigneeName">负责人: {{ t.assigneeName }}</span>
              <span v-if="t.dueDate">截止: {{ formatDate(t.dueDate) }}</span>
              <span>创建: {{ formatDate(t.createdAt) }}</span>
            </div>
          </div>
          <div class="flex flex-col gap-1">
            <button v-if="t.status === 'completed'" @click="reopen(t)" :disabled="busyId === t.id"
                    class="text-xs text-primary hover:underline">重新打开</button>
            <button @click="del(t)" :disabled="busyId === t.id"
                    class="text-xs text-red-500 hover:underline">删除</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import {
  listTasks, createTask, deleteTask, completeTask, reopenTask, adaptTask
} from '@/api/tasks'
import { ElMessage, ElMessageBox } from '@/api/toast'
import {
  PlusIcon, ClipboardDocumentListIcon
} from '@heroicons/vue/24/outline'

const tasks = ref([])
const loading = ref(false)
const busyId = ref(null)
const newTitle = ref('')
const activeTab = ref('all')

const tabs = computed(() => [
  { label: '全部', value: 'all', count: tasks.value.length },
  { label: '进行中', value: 'pending', count: tasks.value.filter(t => t.status !== 'completed').length },
  { label: '已完成', value: 'completed', count: tasks.value.filter(t => t.status === 'completed').length }
])

const filteredTasks = computed(() => {
  if (activeTab.value === 'all') return tasks.value
  if (activeTab.value === 'pending') return tasks.value.filter(t => t.status !== 'completed')
  return tasks.value.filter(t => t.status === 'completed')
})

async function load() {
  loading.value = true
  try {
    const list = await listTasks()
    tasks.value = (list || []).map(adaptTask)
  } catch (e) {
    ElMessage({ message: '加载任务失败：' + (e?.message || ''), type: 'error' })
  } finally {
    loading.value = false
  }
}

async function quickAdd() {
  const title = newTitle.value.trim()
  if (!title) return
  try {
    const t = await createTask({ title, status: 'Pending' })
    tasks.value.unshift(adaptTask(t))
    newTitle.value = ''
  } catch (e) {
    ElMessage({ message: '创建失败：' + (e?.message || ''), type: 'error' })
  }
}

async function toggle(t) {
  busyId.value = t.id
  try {
    if (t.status === 'completed') {
      const r = await reopenTask(t.id)
      const idx = tasks.value.findIndex(x => x.id === t.id)
      if (idx >= 0) tasks.value[idx] = adaptTask(r)
    } else {
      const r = await completeTask(t.id)
      const idx = tasks.value.findIndex(x => x.id === t.id)
      if (idx >= 0) tasks.value[idx] = adaptTask(r)
    }
  } catch (e) {
    ElMessage({ message: '操作失败：' + (e?.message || ''), type: 'error' })
  } finally {
    busyId.value = null
  }
}

async function reopen(t) { await toggle(t) }

async function del(t) {
  try { await ElMessageBox.confirm('确定删除「' + t.title + '」？', '删除任务', { type: 'warning' }) }
  catch { return }
  busyId.value = t.id
  try {
    await deleteTask(t.id)
    tasks.value = tasks.value.filter(x => x.id !== t.id)
    ElMessage({ message: '已删除', type: 'success' })
  } catch (e) {
    ElMessage({ message: '删除失败：' + (e?.message || ''), type: 'error' })
  } finally {
    busyId.value = null
  }
}

function priorityClass(p) {
  return { high: 'bg-red-50 text-red-600', medium: 'bg-amber-50 text-amber-600', low: 'bg-gray-100 text-gray-600' }[p] || 'bg-gray-100 text-gray-500'
}
function priorityLabel(p) { return { high: '高', medium: '中', low: '低' }[p] || '普通' }
function statusClass(s) {
  return s === 'completed' ? 'bg-green-50 text-green-600' :
         s === 'in_progress' ? 'bg-blue-50 text-blue-600' :
         'bg-gray-100 text-gray-600'
}
function statusLabel(s) { return { completed: '已完成', in_progress: '进行中', pending: '待办' }[s] || s }
function formatDate(iso) {
  if (!iso) return ''
  return new Date(iso).toISOString().slice(0, 10)
}

onMounted(load)
</script>
