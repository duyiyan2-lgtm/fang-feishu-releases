<template>
  <div class="flex flex-col h-full bg-[#F7F8FA] dark:bg-[#12151C] transition-colors duration-200">
    <div class="h-13 px-5 flex items-center justify-between border-b border-line dark:border-gray-700/80 bg-white/90 dark:bg-gray-900/90 backdrop-blur-sm flex-shrink-0 gap-3">
      <div class="flex items-center gap-1 p-0.5 rounded-lg bg-surface-secondary dark:bg-gray-800 border border-line-soft dark:border-gray-700">
        <button
          v-for="t in tabs"
          :key="t.value"
          type="button"
          class="px-3 h-7 text-sm rounded-md transition-all duration-150 font-medium"
          :class="activeTab === t.value
            ? 'bg-primary text-white shadow-sm'
            : 'hover:bg-white dark:hover:bg-gray-700 text-ink-secondary dark:text-gray-300'"
          @click="activeTab = t.value"
        >
          {{ t.label }}
          <span v-if="t.count" class="ml-1 text-2xs opacity-80">({{ t.count }})</span>
        </button>
      </div>
      <div class="flex items-center gap-2">
        <div class="relative">
          <MagnifyingGlassIcon class="absolute left-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-ink-tertiary pointer-events-none" />
          <input v-model="search" placeholder="搜索审批" class="ff-input pl-9 w-52" />
        </div>
        <button type="button" class="ff-btn-primary" @click="$router.push('/approvals/new')">
          <PlusIcon class="w-4 h-4" />发起审批
        </button>
      </div>
    </div>

    <div class="px-5 py-2.5 border-b border-line-soft dark:border-gray-800 flex items-center gap-1.5 flex-shrink-0 bg-white dark:bg-gray-900">
      <button
        v-for="s in statuses"
        :key="s.value"
        type="button"
        class="px-3 h-7 text-xs rounded-full transition-all font-medium"
        :class="activeStatus === s.value
          ? 'bg-primary-soft dark:bg-primary/20 text-primary'
          : 'hover:bg-surface-secondary dark:hover:bg-gray-800 text-ink-secondary dark:text-gray-300'"
        @click="activeStatus = s.value"
      >
        {{ s.label }}
      </button>
    </div>

    <div class="flex-1 overflow-y-auto scrollbar-auto px-5 py-4">
      <div v-if="loading" class="p-12 text-center text-sm text-ink-tertiary">
        <svg class="animate-spin w-5 h-5 mx-auto mb-2 text-primary" viewBox="0 0 24 24" fill="none">
          <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
          <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
        </svg>
        加载中…
      </div>

      <div v-else-if="filteredApprovals.length === 0" class="text-center py-20">
        <div class="w-16 h-16 mx-auto rounded-2xl bg-primary-soft dark:bg-primary/10 flex items-center justify-center mb-3">
          <ClipboardDocumentCheckIcon class="w-8 h-8 text-primary/50" />
        </div>
        <p class="text-sm text-ink-tertiary">暂无审批记录</p>
      </div>

      <div v-else class="bg-white dark:bg-gray-900 border border-line dark:border-gray-700/80 rounded-xl shadow-soft overflow-hidden">
        <table class="w-full text-sm">
          <thead class="text-2xs text-ink-tertiary bg-surface-secondary/80 dark:bg-gray-800/60 sticky top-0 z-[1]">
            <tr>
              <th class="text-left py-3 px-5 font-semibold w-28">编号</th>
              <th class="text-left py-3 px-3 font-semibold">类型</th>
              <th class="text-left py-3 px-3 font-semibold">标题</th>
              <th class="text-left py-3 px-3 font-semibold w-32">申请人</th>
              <th class="text-left py-3 px-3 font-semibold w-36">提交时间</th>
              <th class="text-left py-3 px-3 font-semibold w-28">状态</th>
              <th class="text-left py-3 px-5 font-semibold w-28">操作</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="a in filteredApprovals"
              :key="a.id"
              class="border-t border-line-soft dark:border-gray-800 hover:bg-primary-soft/40 dark:hover:bg-primary/10 cursor-pointer transition-colors"
              @click="$router.push(`/approvals/${a.id}`)"
            >
              <td class="py-3 px-5 text-xs font-mono text-ink-tertiary">{{ a.id.slice(0, 8) }}</td>
              <td class="py-3 px-3">
                <div class="flex items-center">
                  <span class="text-base mr-1.5">{{ getTypeIcon(a.typeKey) }}</span>
                  <span class="text-xs text-ink-secondary dark:text-gray-300">{{ a.type }}</span>
                </div>
              </td>
              <td class="py-3 px-3">
                <span class="font-medium text-ink dark:text-gray-100">{{ a.title }}</span>
              </td>
              <td class="py-3 px-3">
                <div class="flex items-center">
                  <div class="w-6 h-6 rounded-full text-xs text-white flex items-center justify-center mr-2 font-semibold" :style="{ background: a.applicantColor }">{{ a.applicant[0] }}</div>
                  <span class="text-ink-secondary dark:text-gray-300 text-sm">{{ a.applicant }}</span>
                </div>
              </td>
              <td class="py-3 px-3 text-ink-tertiary text-sm">{{ a.createdAt }}</td>
              <td class="py-3 px-3">
                <span class="px-2 py-0.5 rounded-full text-2xs font-semibold" :class="statusClass(a.status)">{{ statusLabel(a.status) }}</span>
              </td>
              <td class="py-3 px-5">
                <div class="flex items-center gap-2">
                  <button v-if="a.status === 'pending' && a.applicantId !== meId" type="button" class="text-xs text-amber-500 hover:underline font-medium" @click.stop="onRemind(a)">催办</button>
                  <button v-if="a.status === 'pending' && a.applicantId === meId" type="button" class="text-xs text-red-500 hover:underline font-medium" @click.stop="onWithdraw(a)">撤回</button>
                  <button type="button" class="text-xs text-primary hover:underline font-medium" @click.stop="goProcess(a)">
                    {{ a.status === 'pending' ? '处理' : '查看' }}
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup>
defineOptions({ name: 'ApprovalList' })

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