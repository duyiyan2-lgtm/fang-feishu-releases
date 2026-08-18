<template>
  <div class="flex flex-col h-full">
    <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700 flex-shrink-0">
      <div>
        <h2 class="text-base font-medium text-gray-900 dark:text-gray-100">应用鉴权</h2>
        <p class="text-xs text-gray-500 mt-0.5">管理第三方应用的 AppID / AppSecret 及权限范围</p>
      </div>
      <button class="h-8 px-3 text-sm bg-primary hover:bg-primary-hover text-white rounded-md flex items-center">
        <PlusIcon class="w-4 h-4 mr-1" />创建应用
      </button>
    </div>

    <div class="flex-1 overflow-y-auto p-6">
      <div class="max-w-5xl mx-auto space-y-3">
        <div v-for="app in apps" :key="app.id"
             class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-5 hover:shadow-sm transition">
          <div class="flex items-start justify-between">
            <div class="flex-1">
              <div class="flex items-center space-x-2">
                <h3 class="text-base font-medium text-gray-900 dark:text-gray-100">{{ app.name }}</h3>
                <span :class="['px-2 py-0.5 rounded-full text-xs font-medium',
                              app.status === 'active' ? 'bg-green-50 text-green-600 dark:bg-green-500/20 dark:text-green-300' : 'bg-gray-100 text-gray-500']">
                  {{ app.status === 'active' ? '已启用' : '已禁用' }}
                </span>
              </div>
              <div class="mt-2 flex flex-wrap gap-x-6 gap-y-1 text-sm">
                <div>
                  <span class="text-xs text-gray-500">AppID</span>
                  <code class="ml-1 font-mono text-xs text-gray-700 dark:text-gray-200">{{ app.appId }}</code>
                  <button @click="copy(app.appId)" class="ml-1 text-xs text-primary hover:underline">复制</button>
                </div>
                <div>
                  <span class="text-xs text-gray-500">AppSecret</span>
                  <code class="ml-1 font-mono text-xs text-gray-700 dark:text-gray-200">{{ app.secret }}</code>
                  <button @click="copy(app.secret, '请妥善保管！')" class="ml-1 text-xs text-primary hover:underline">复制</button>
                </div>
                <div>
                  <span class="text-xs text-gray-500">创建于 {{ app.createdAt }}</span>
                </div>
              </div>
              <div class="mt-3">
                <div class="text-xs text-gray-500 mb-1.5">权限范围（Scope）</div>
                <div class="flex flex-wrap gap-1.5">
                  <span v-for="s in app.scope" :key="s" class="px-2 py-0.5 rounded text-xs bg-purple-50 text-purple-600 dark:bg-purple-500/20 dark:text-purple-300 font-mono">{{ s }}</span>
                </div>
              </div>
            </div>
            <div class="flex items-center space-x-1 ml-4">
              <button class="px-2.5 h-7 text-xs border border-gray-200 dark:border-gray-700 rounded hover:bg-gray-50 dark:hover:bg-gray-700 dark:text-gray-200">编辑</button>
              <button @click="toggle(app)" class="px-2.5 h-7 text-xs border border-gray-200 dark:border-gray-700 rounded hover:bg-gray-50 dark:hover:bg-gray-700 dark:text-gray-200">
                {{ app.status === 'active' ? '禁用' : '启用' }}
              </button>
              <button class="px-2.5 h-7 text-xs border border-gray-200 dark:border-gray-700 rounded hover:bg-red-50 hover:text-red-500 text-red-500">删除</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { mockPlatformApps } from '@/api/mock'
import { ElMessage } from '@/api/toast'
import { PlusIcon } from '@heroicons/vue/24/outline'

const apps = ref(JSON.parse(JSON.stringify(mockPlatformApps)))

function toggle(a) { a.status = a.status === 'active' ? 'disabled' : 'active' }
function copy(value, warn = '') {
  navigator.clipboard?.writeText(value)
  ElMessage({ message: `已复制到剪贴板${warn ? '（' + warn + '）' : ''}`, type: 'success' })
}
</script>
