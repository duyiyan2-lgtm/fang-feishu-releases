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
        <div class="relative">
          <MagnifyingGlassIcon class="absolute left-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <input v-model="search" placeholder="搜索审批"
                 class="h-8 pl-9 pr-3 text-sm bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-md outline-none focus:ring-2 focus:ring-primary/30 w-56 dark:text-gray-100" />
        </div>
        <button @click="$router.push('/approvals/new')"
                class="h-8 px-3 text-sm bg-primary hover:bg-primary-hover text-white rounded-md flex items-center transition">
          <PlusIcon class="w-4 h-4 mr-1" />发起审批
        </button>
      </div>
    </div>

    <div class="px-6 py-2 border-b border-gray-100 dark:border-gray-800 flex items-center space-x-1 flex-shrink-0">
      <button v-for="s in statuses" :key="s.value" @click="activeStatus = s.value"
              :class="['px-3 h-7 text-xs rounded transition',
                       activeStatus === s.value
                         ? 'bg-primary-50 dark:bg-primary/20 text-primary dark:text-primary-100'
                         : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-600 dark:text-gray-300']">
        {{ s.label }}
      </button>
    </div>

    <div class="flex-1 overflow-y-auto">
      <div v-if="loading" class="p-12 text-center text-sm text-gray-400">
        <svg class="animate-spin w-5 h-5 mx-auto mb-2" viewBox="0 0 24 24" fill="none">
          <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
          <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
        </svg>
        加载中…
      </div>
      <table v-else class="w-full text-sm">
        <thead class="text-xs text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800/50 sticky top-0">
          <tr>
            <th class="text-left py-3 px-6 font-medium w-32">编号</th>
            <th class="text-left py-3 px-3 font-medium">类型</th>
            <th class="text-left py-3 px-3 font-medium">标题</th>
            <th class="text-left py-3 px-3 font-medium w-32">申请人</th>
            <th class="text-left py-3 px-3 font-medium w-36">提交时间</th>
            <th class="text-left py-3 px-3 font-medium w-28">状态</th>
            <th class="text-left py-3 px-6 font-medium w-24">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="a in filteredApprovals" :key="a.id"
              @click="$router.push(`/approvals/${a.id}`)"
              class="border-b border-gray-100 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-800/50 cursor-pointer transition-colors">
            <td class="py-3 px-6 text-xs font-mono text-gray-500 dark:text-gray-400">{{ a.id.slice(0, 8) }}</td>
            <td class="py-3 px-3">
              <div class="flex items-center">
                <span class="text-base mr-1.5">{{ getTypeIcon(a.typeKey) }}</span>
                <span class="text-xs text-gray-600 dark:text-gray-300">{{ a.type }}</span>
              </div>
            </td>
            <td class="py-3 px-3">
              <span class="font-medium text-gray-900 dark:text-gray-100">{{ a.title }}</span>
            </td>
            <td class="py-3 px-3">
              <div class="flex items-center">
                <div class="w-6 h-6 rounded-full text-xs text-white flex items-center justify-center mr-2" :style="{ background: a.applicantColor }">{{ a.applicant[0] }}</div>
                <span class="text-gray-700 dark:text-gray-300 text-sm">{{ a.applicant }}</span>
              </div>
            </td>
            <td class="py-3 px-3 text-gray-500 text-sm">{{ a.createdAt }}</td>
            <td class="py-3 px-3">
              <span :class="['px-2 py-0.5 rounded text-xs font-medium', statusClass(a.status)]">{{ statusLabel(a.status) }}</span>
            </td>
            <td class="py-3 px-6 flex items-center gap-2">
              <button v-if="a.status === 'pending' && a.applicantId !== meId" @click.stop="onRemind(a)" class="text-xs text-amber-500 hover:underline">催办</button>
              <button v-if="a.status === 'pending' && a.applicantId === meId" @click.stop="onWithdraw(a)" class="text-xs text-red-500 hover:underline">撤回</button>
              <button @click.stop="goProcess(a)" class="text-xs text-primary hover:underline">
                {{ a.status === 'pending' ? '处理' : '查看' }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      <div v-if="!loading && filteredApprovals.length === 0" class="text-center py-20">
        <ClipboardDocumentCheckIcon class="w-12 h-12 mx-auto text-gray-300 dark:text-gray-700 mb-3" />
        <p class="text-sm text-gray-500">暂无审批记录</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { listApprovals, adaptApproval, remindApproval, withdrawApproval } from '@/api/approvals'
import { useUserStore } from '@/stores/user'
import { ElMessage, ElMessageBox } from '@/api/toast'
import {
  MagnifyingGlassIcon, PlusIcon, ClipboardDocumentCheckIcon
} from '@heroicons/vue/24/outline'

const router = useRouter()
const userStore = useUserStore()
const meId = computed(() => userStore.userInfo?.id)
const search = ref('')
const activeTab = ref('todo')
const activeStatus = ref('all')
const loading = ref(true)
const approvals = ref([])

async function onRemind(a) {
  try {
    await remindApproval(a.id)
    ElMessage({ message: '已催办 ' + a.applicant, type: 'success' })
  } catch (e) {
    ElMessage({ message: '催办失败：' + (e?.message || ''), type: 'error' })
  }
}
async function onWithdraw(a) {
  try {
    await ElMessageBox.confirm('确定撤回「' + a.title + '」？', '撤回审批', { type: 'warning' })
  } catch { return }
  try {
    await withdrawApproval(a.id)
    ElMessage({ message: '已撤回', type: 'success' })
    await load()
  } catch (e) {
    ElMessage({ message: '撤回失败：' + (e?.message || ''), type: 'error' })
  }
}

const tabs = computed(() => [
  { label: '待我审批', value: 'todo', count: approvals.value.filter(a => a.status === 'pending').length },
  { label: '我发起的', value: 'mine', count: approvals.value.filter(a => a.applicantId === userStore.userInfo?.id).length },
  { label: '全部',     value: 'all',  count: approvals.value.length }
])

const statuses = [
  { label: '全部状态', value: 'all' },
  { label: '待审批',   value: 'pending' },
  { label: '已通过',   value: 'approved' },
  { label: '已驳回',   value: 'rejected' }
]

const filteredApprovals = computed(() => {
  let list = approvals.value
  if (activeTab.value === 'todo') list = list.filter(a => a.status === 'pending')
  else if (activeTab.value === 'mine') list = list.filter(a => a.applicantId === userStore.userInfo?.id)
  if (activeStatus.value !== 'all') list = list.filter(a => a.status === activeStatus.value)
  const kw = search.value.trim().toLowerCase()
  if (kw) list = list.filter(a => (a.title || '').toLowerCase().includes(kw) || (a.applicant || '').toLowerCase().includes(kw))
  return list
})

function getTypeIcon(key) {
  return { leave: '🌴', expense: '💰', trip: '✈️', overtime: '⏰', seal: '🔖', goods: '📦' }[key] || '📋'
}
function statusLabel(s) { return { pending: '待审批', approved: '已通过', rejected: '已驳回' }[s] || s }
function statusClass(s) {
  return {
    pending:  'bg-orange-50 text-orange-600 dark:bg-orange-500/20 dark:text-orange-300',
    approved: 'bg-green-50 text-green-600 dark:bg-green-500/20 dark:text-green-300',
    rejected: 'bg-red-50 text-red-600 dark:bg-red-500/20 dark:text-red-300'
  }[s] || 'bg-gray-100 text-gray-500'
}
function goProcess(a) { router.push(`/approvals/${a.id}`) }

async function load() {
  loading.value = true
  try {
    const list = await listApprovals()
    approvals.value = (list || []).map(adaptApproval)
  } catch (e) {
    console.error('[approvals] load failed', e)
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>