<template>
  <div v-if="approval" class="flex flex-col h-full bg-gray-50 dark:bg-[#0E1116] transition-colors">
    <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 flex-shrink-0">
      <div class="flex items-center space-x-3">
        <button @click="$router.back()" class="w-8 h-8 rounded hover:bg-gray-100 dark:hover:bg-gray-800 flex items-center justify-center transition">
          <ArrowLeftIcon class="w-4 h-4 text-gray-600 dark:text-gray-300" />
        </button>
        <h1 class="text-base font-medium text-gray-900 dark:text-gray-100">审批详情</h1>
        <span class="text-xs font-mono text-gray-400">{{ approval.id.slice(0, 8) }}</span>
      </div>
      <span :class="['px-2.5 py-0.5 rounded text-xs font-medium', statusClass(approval.status)]">{{ statusLabel(approval.status) }}</span>
    </div>

    <div class="flex-1 overflow-y-auto">
      <div class="max-w-4xl mx-auto p-6 space-y-4">
        <div class="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
          <h2 class="text-base font-medium text-gray-900 dark:text-gray-100 mb-3">{{ approval.title }}</h2>
          <div class="flex items-center text-sm text-gray-500 dark:text-gray-400 mb-5 space-x-4">
            <div class="flex items-center">
              <span class="text-base mr-1.5">{{ getTypeIcon(approval.typeKey) }}</span>
              {{ approval.type }}
            </div>
            <span>·</span>
            <span>{{ approval.applicant }}</span>
            <span>·</span>
            <span>{{ approval.createdAt }}</span>
          </div>

          <h3 class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3 pb-2 border-b border-gray-100 dark:border-gray-800">申请内容</h3>
          <dl class="grid grid-cols-1 md:grid-cols-2 gap-y-3 text-sm">
            <div v-for="f in approval.fields" :key="f.key" class="flex">
              <dt class="w-24 text-gray-500 flex-shrink-0">{{ f.label }}</dt>
              <dd class="text-gray-900 dark:text-gray-100 break-all">{{ f.value }}</dd>
            </div>
          </dl>
        </div>

        <div class="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
          <h3 class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-4 pb-2 border-b border-gray-100 dark:border-gray-800">审批记录</h3>
          <div v-if="!approval.flow.length" class="text-sm text-gray-400 py-4 text-center">暂无审批记录</div>
          <div v-else class="space-y-5">
            <div v-for="(node, idx) in approval.flow" :key="idx" class="flex gap-3">
              <div class="flex flex-col items-center">
                <div :class="['w-9 h-9 rounded-full flex items-center justify-center text-white text-sm font-medium shadow-sm', flowBadgeClass(node.status)]">
                  <CheckIcon v-if="node.status === 'approved'" class="w-4 h-4" />
                  <XMarkIcon v-else-if="node.status === 'rejected'" class="w-4 h-4" />
                  <span v-else>{{ idx + 1 }}</span>
                </div>
              </div>
              <div class="flex-1 pb-2">
                <div class="flex items-center justify-between">
                  <div>
                    <div class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ node.person }}</div>
                    <div class="text-xs text-gray-500 mt-0.5">{{ node.node }}</div>
                  </div>
                  <span class="text-xs text-gray-400">{{ node.time }}</span>
                </div>
                <div v-if="node.comment" class="mt-2 px-3 py-2 bg-gray-50 dark:bg-gray-800 rounded text-sm text-gray-700 dark:text-gray-200">
                  {{ node.comment }}
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 操作区：仅申请人不是自己时显示「处理」按钮 -->
        <div v-if="approval.status === 'pending' && approval.applicantId !== currentUserId" class="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
          <h3 class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-4 pb-2 border-b border-gray-100 dark:border-gray-800">审批操作</h3>
          <textarea v-model="comment" placeholder="请输入审批意见（可选）" rows="3"
                    class="w-full px-3 py-2 text-sm border border-gray-200 dark:border-gray-700 rounded-md bg-gray-50 dark:bg-gray-800/50 outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 dark:text-gray-100" />
          <div class="mt-3 flex flex-wrap gap-2">
            <button @click="act('approve')" :disabled="acting" class="h-9 px-4 bg-primary text-white rounded-md hover:bg-primary-hover transition flex items-center text-sm disabled:opacity-60">
              <CheckIcon class="w-4 h-4 mr-1.5" />同意
            </button>
            <button @click="act('reject')" :disabled="acting" class="h-9 px-4 bg-red-500 text-white rounded-md hover:bg-red-600 transition flex items-center text-sm disabled:opacity-60">
              <XMarkIcon class="w-4 h-4 mr-1.5" />驳回
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>

  <div v-else-if="loading" class="flex h-full items-center justify-center text-gray-400 text-sm">加载中…</div>
  <div v-else class="flex h-full items-center justify-center text-gray-400 text-sm">审批不存在</div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { listApprovals, adaptApproval, approveApproval, rejectApproval } from '@/api/approvals'
import { useUserStore } from '@/stores/user'
import { ElMessage } from '@/api/toast'
import {
  ArrowLeftIcon, CheckIcon, XMarkIcon
} from '@heroicons/vue/24/outline'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const comment = ref('')
const loading = ref(true)
const acting = ref(false)
const approval = ref(null)

const currentUserId = computed(() => userStore.userInfo?.id)

function getTypeIcon(key) {
  return { leave: '🌴', expense: '💰', trip: '✈️', overtime: '⏰', seal: '🔖', goods: '📦' }[key] || '📋'
}
function statusLabel(s) { return { pending: '待审批', approved: '已通过', rejected: '已驳回' }[s] || s }
function statusClass(s) {
  return {
    pending: 'bg-orange-50 text-orange-600 dark:bg-orange-500/20 dark:text-orange-300',
    approved: 'bg-green-50 text-green-600 dark:bg-green-500/20 dark:text-green-300',
    rejected: 'bg-red-50 text-red-600 dark:bg-red-500/20 dark:text-red-300'
  }[s]
}
function flowBadgeClass(s) {
  return {
    approved: 'bg-green-500',
    rejected: 'bg-red-500'
  }[s] || 'bg-primary ring-4 ring-primary/20'
}

async function load() {
  loading.value = true
  try {
    const list = await listApprovals()
    const found = (list || []).find(a => a.id === route.params.id)
    approval.value = found ? adaptApproval(found) : null
  } catch (e) {
    ElMessage({ message: '加载审批失败', type: 'error' })
  } finally {
    loading.value = false
  }
}

async function act(type) {
  if (!approval.value) return
  acting.value = true
  try {
    if (type === 'approve') {
      await approveApproval(approval.value.id, comment.value)
      ElMessage({ message: '已通过审批', type: 'success' })
    } else {
      if (!comment.value.trim()) {
        ElMessage({ message: '请填写驳回意见', type: 'warning' })
        return
      }
      await rejectApproval(approval.value.id, comment.value)
      ElMessage({ message: '已驳回', type: 'success' })
    }
    await load()
  } catch (e) {
    ElMessage({ message: (type === 'approve' ? '通过' : '驳回') + '失败：' + (e.message || ''), type: 'error' })
  } finally {
    acting.value = false
  }
}

onMounted(load)
</script>