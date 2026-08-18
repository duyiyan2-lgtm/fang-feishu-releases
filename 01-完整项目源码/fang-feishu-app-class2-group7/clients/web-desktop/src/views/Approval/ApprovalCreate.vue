<template>
  <div class="flex flex-col h-full bg-gray-50 dark:bg-[#0E1116] transition-colors">
    <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 flex-shrink-0">
      <div class="flex items-center space-x-3">
        <button @click="$router.back()" class="w-8 h-8 rounded hover:bg-gray-100 dark:hover:bg-gray-800 flex items-center justify-center">
          <ArrowLeftIcon class="w-4 h-4 text-gray-600 dark:text-gray-300" />
        </button>
        <h1 class="text-base font-medium text-gray-900 dark:text-gray-100">发起审批</h1>
      </div>
      <div class="flex items-center space-x-2">
        <button class="h-8 px-4 text-sm border border-gray-200 dark:border-gray-700 rounded-md hover:bg-gray-50 dark:hover:bg-gray-800 dark:text-gray-200">存为草稿</button>
        <button @click="submit" :disabled="saving" class="h-8 px-4 text-sm bg-primary hover:bg-primary-hover text-white rounded-md disabled:opacity-60 transition">
          {{ saving ? '提交中…' : '提交' }}
        </button>
      </div>
    </div>

    <div class="flex-1 overflow-y-auto">
      <div class="max-w-3xl mx-auto p-6 space-y-4">
        <!-- 选择审批类型 -->
        <div class="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
          <h3 class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-4 pb-2 border-b border-gray-100 dark:border-gray-800">选择审批类型</h3>
          <div class="grid grid-cols-3 md:grid-cols-4 gap-3">
            <button v-for="t in typeOptions" :key="t.value" @click="form.type = t.value"
                    class="p-4 border-2 rounded-lg text-center transition"
                    :class="form.type === t.value
                      ? 'border-primary bg-primary-50 dark:bg-primary/20'
                      : 'border-gray-200 dark:border-gray-700 hover:border-primary/50'">
              <div class="text-2xl mb-1">{{ t.icon }}</div>
              <div class="text-xs text-gray-700 dark:text-gray-200">{{ t.label }}</div>
            </button>
          </div>
        </div>

        <!-- 表单 -->
        <div class="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
          <h3 class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-4 pb-2 border-b border-gray-100 dark:border-gray-800">填写申请内容</h3>

          <div v-if="!form.type" class="text-center py-12 text-sm text-gray-400">请先选择审批类型</div>

          <div v-else class="space-y-4">
            <div>
              <label class="block text-sm text-gray-700 dark:text-gray-300 mb-1">标题 *</label>
              <input v-model="form.title" placeholder="如：请假申请 - 病假 2 天"
                     class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md bg-white dark:bg-gray-800/50 outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 dark:text-gray-100" />
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm text-gray-700 dark:text-gray-300 mb-1">开始 *</label>
                <input v-model="form.startDateTime" type="datetime-local" class="input" />
              </div>
              <div>
                <label class="block text-sm text-gray-700 dark:text-gray-300 mb-1">结束 *</label>
                <input v-model="form.endDateTime" type="datetime-local" class="input" />
              </div>
            </div>

            <div>
              <label class="block text-sm text-gray-700 dark:text-gray-300 mb-1">事由 *</label>
              <textarea v-model="form.reason" rows="3" placeholder="请详细说明原因" class="input" />
            </div>

            <div>
              <label class="block text-sm text-gray-700 dark:text-gray-300 mb-1">地点（可选）</label>
              <input v-model="form.location" class="input" />
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { createApproval } from '@/api/approvals'
import { ElMessage } from '@/api/toast'
import { ArrowLeftIcon } from '@heroicons/vue/24/outline'
import dayjs from '@/utils/dayjs'

const router = useRouter()
const saving = ref(false)

const typeOptions = [
  { value: 'leave', label: '请假申请', icon: '🌴' },
  { value: 'expense', label: '报销申请', icon: '💰' },
  { value: 'trip', label: '出差申请', icon: '✈️' },
  { value: 'overtime', label: '加班申请', icon: '⏰' },
  { value: 'seal', label: '用印申请', icon: '🔖' }
]

const form = reactive({
  type: 'leave',
  title: '',
  startDateTime: dayjs().add(1, 'day').hour(9).minute(0).second(0).format('YYYY-MM-DDTHH:mm'),
  endDateTime: dayjs().add(1, 'day').hour(18).minute(0).second(0).format('YYYY-MM-DDTHH:mm'),
  reason: '',
  location: ''
})

async function submit() {
  if (!form.title) return ElMessage({ message: '请输入标题', type: 'warning' })
  if (!form.startDateTime || !form.endDateTime) return ElMessage({ message: '请填写时间', type: 'warning' })
  if (!form.reason) return ElMessage({ message: '请填写事由', type: 'warning' })
  saving.value = true
  try {
    // 后端 ApprovalRequest = { type, title, content }
    // 把时间段 + 事由塞进 content 里（后端 content 是 string）
    const start = dayjs(form.startDateTime).format('YYYY-MM-DD HH:mm')
    const end = dayjs(form.endDateTime).format('YYYY-MM-DD HH:mm')
    const content = `【${start} 至 ${end}】${form.location ? '📍' + form.location + ' ' : ''}${form.reason}`
    await createApproval({
      type: form.type,
      title: form.title,
      content
    })
    ElMessage({ message: '审批已提交', type: 'success' })
    router.push('/approvals')
  } catch (e) {
    ElMessage({ message: '提交失败：' + (e.message || ''), type: 'error' })
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.input {
  @apply w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md bg-white dark:bg-gray-800/50 outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 dark:text-gray-100;
}
</style>