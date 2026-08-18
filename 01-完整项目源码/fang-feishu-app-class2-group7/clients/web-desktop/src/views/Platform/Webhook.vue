<template>
  <div class="flex flex-col h-full">
    <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700 flex-shrink-0">
      <div>
        <h2 class="text-base font-medium text-gray-900 dark:text-gray-100">Webhook 配置</h2>
        <p class="text-xs text-gray-500 mt-0.5">通过 Webhook 将事件推送到外部系统</p>
      </div>
      <button @click="create" class="h-8 px-3 text-sm bg-primary hover:bg-primary-hover text-white rounded-md flex items-center">
        <PlusIcon class="w-4 h-4 mr-1" />新建 Webhook
      </button>
    </div>

    <div class="flex-1 overflow-y-auto p-6">
      <div class="max-w-5xl mx-auto space-y-3">
        <div v-for="w in webhooks" :key="w.id"
             class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4 hover:shadow-sm transition">
          <div class="flex items-center justify-between">
            <div class="flex items-center space-x-3">
              <div :class="['w-2 h-2 rounded-full', w.status === 'active' ? 'bg-green-500' : 'bg-gray-400']"></div>
              <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ w.name }}</h3>
              <span :class="['px-2 py-0.5 rounded text-xs',
                            w.status === 'active'
                              ? 'bg-green-50 text-green-600 dark:bg-green-500/20 dark:text-green-300'
                              : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300']">
                {{ w.status === 'active' ? '已启用' : '已暂停' }}
              </span>
            </div>
            <div class="flex items-center space-x-1">
              <button @click="toggle(w)" class="px-2.5 h-7 text-xs border border-gray-200 dark:border-gray-700 rounded hover:bg-gray-50 dark:hover:bg-gray-700 dark:text-gray-200">
                {{ w.status === 'active' ? '暂停' : '启用' }}
              </button>
              <button class="px-2.5 h-7 text-xs border border-gray-200 dark:border-gray-700 rounded hover:bg-gray-50 dark:hover:bg-gray-700 dark:text-gray-200">日志</button>
              <button class="px-2.5 h-7 text-xs border border-gray-200 dark:border-gray-700 rounded hover:bg-gray-50 dark:hover:bg-gray-700 dark:text-gray-200">编辑</button>
              <button @click="del(w)" class="px-2.5 h-7 text-xs border border-gray-200 dark:border-gray-700 rounded hover:bg-red-50 hover:text-red-500 hover:border-red-200 text-red-500">删除</button>
            </div>
          </div>
          <div class="mt-3 grid grid-cols-2 gap-4 text-sm">
            <div>
              <span class="text-xs text-gray-500">URL</span>
              <div class="mt-1 font-mono text-xs text-gray-700 dark:text-gray-200 bg-gray-50 dark:bg-gray-900 px-2 py-1 rounded">{{ w.url }}</div>
            </div>
            <div>
              <span class="text-xs text-gray-500">Secret</span>
              <div class="mt-1 font-mono text-xs text-gray-700 dark:text-gray-200 bg-gray-50 dark:bg-gray-900 px-2 py-1 rounded">{{ w.secret }}</div>
            </div>
          </div>
          <div class="mt-3 flex flex-wrap gap-1.5">
            <span v-for="ev in w.event" :key="ev" class="px-2 py-0.5 rounded text-xs bg-blue-50 text-blue-600 dark:bg-blue-500/20 dark:text-blue-300 font-mono">{{ ev }}</span>
          </div>
          <div class="mt-3 text-xs text-gray-400">创建于 {{ w.createdAt }}</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { mockWebhooks } from '@/api/mock'
import { ElMessage } from '@/api/toast'
import { PlusIcon } from '@heroicons/vue/24/outline'

const webhooks = ref(JSON.parse(JSON.stringify(mockWebhooks)))

function toggle(w) { w.status = w.status === 'active' ? 'paused' : 'active' }
function del(w) {
  webhooks.value = webhooks.value.filter(x => x.id !== w.id)
  ElMessage({ message: '已删除 Webhook', type: 'success' })
}
function create() { ElMessage({ message: '新建 Webhook 表单打开（示意）', type: 'info' }) }
</script>
