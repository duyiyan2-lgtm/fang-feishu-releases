<template>
  <div class="flex flex-col h-full">
    <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700 flex-shrink-0">
      <h2 class="text-base font-medium text-gray-900 dark:text-gray-100">操作日志 ({{ logs.length }})</h2>
      <div class="flex items-center space-x-2">
        <input v-model="search" placeholder="搜索"
               class="h-8 px-3 text-sm bg-gray-100 dark:bg-gray-800 rounded-md outline-none w-48 dark:text-gray-100" />
        <button class="h-8 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md hover:bg-gray-50 dark:hover:bg-gray-800 dark:text-gray-200">导出</button>
      </div>
    </div>

    <div class="flex-1 overflow-y-auto">
      <table class="w-full text-sm">
        <thead class="text-xs text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800/50 sticky top-0">
          <tr>
            <th class="text-left py-3 px-6 font-medium w-24">编号</th>
            <th class="text-left py-3 px-3 font-medium w-28">模块</th>
            <th class="text-left py-3 px-3 font-medium w-28">操作</th>
            <th class="text-left py-3 px-3 font-medium">详情</th>
            <th class="text-left py-3 px-3 font-medium w-28">操作人</th>
            <th class="text-left py-3 px-3 font-medium w-32">IP</th>
            <th class="text-left py-3 px-3 font-medium w-24">结果</th>
            <th class="text-left py-3 px-6 font-medium w-28">时间</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="log in filteredLogs" :key="log.id" class="border-b border-gray-100 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-800/50 transition">
            <td class="py-3 px-6 text-xs font-mono text-gray-500">{{ log.id }}</td>
            <td class="py-3 px-3">
              <span class="px-2 py-0.5 rounded text-xs bg-blue-50 dark:bg-blue-500/20 text-blue-700 dark:text-blue-300">{{ log.module }}</span>
            </td>
            <td class="py-3 px-3 text-gray-700 dark:text-gray-300">{{ log.action }}</td>
            <td class="py-3 px-3 text-gray-600 dark:text-gray-300">{{ log.target }}</td>
            <td class="py-3 px-3 text-gray-700 dark:text-gray-300">{{ log.user }}</td>
            <td class="py-3 px-3 text-xs font-mono text-gray-500">{{ log.ip }}</td>
            <td class="py-3 px-3">
              <span :class="['inline-flex items-center px-2 py-0.5 rounded text-xs',
                            log.result === 'success' ? 'bg-green-50 text-green-600 dark:bg-green-500/20 dark:text-green-300' : 'bg-red-50 text-red-600 dark:bg-red-500/20 dark:text-red-300']">
                <CheckCircleIcon v-if="log.result === 'success'" class="w-3 h-3 mr-1" />
                <XCircleIcon v-else class="w-3 h-3 mr-1" />
                {{ log.result === 'success' ? '成功' : '失败' }}
              </span>
            </td>
            <td class="py-3 px-6 text-gray-500 text-sm">{{ log.time }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { listOperationLogs, adaptLog } from '@/api/logs'
import { ElMessage } from '@/api/toast'
import { CheckCircleIcon, XCircleIcon } from '@heroicons/vue/24/outline'

const logs = ref([])
const search = ref('')
const loading = ref(false)

const filteredLogs = computed(() => {
  const kw = search.value.trim().toLowerCase()
  if (!kw) return logs.value
  return logs.value.filter(l =>
    (l.module || '').toLowerCase().includes(kw) ||
    (l.action || '').toLowerCase().includes(kw) ||
    (l.user || '').toLowerCase().includes(kw) ||
    (l.target || '').toLowerCase().includes(kw)
  )
})

async function load() {
  loading.value = true
  try {
    const data = await listOperationLogs()
    logs.value = (data.items || []).map(adaptLog)
  } catch (e) {
    ElMessage({ message: '加载日志失败：' + (e?.message || ''), type: 'error' })
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>
