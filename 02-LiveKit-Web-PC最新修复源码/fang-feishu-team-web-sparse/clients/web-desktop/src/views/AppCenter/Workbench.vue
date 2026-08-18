<template>
  <div class="flex h-full bg-white dark:bg-gray-900 transition-colors">
    <!-- 左：可选应用 -->
    <div class="w-72 border-r border-gray-200 dark:border-gray-700 flex flex-col bg-gray-50 dark:bg-[#1A1D23] flex-shrink-0">
      <div class="p-4 border-b border-gray-200 dark:border-gray-700">
        <h3 class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">待添加应用</h3>
        <input v-model="search" placeholder="搜索应用"
               class="w-full h-8 px-3 text-sm bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-md outline-none focus:border-primary dark:text-gray-100" />
      </div>
      <div class="flex-1 overflow-y-auto p-3 space-y-2">
        <div v-for="app in availableApps" :key="app.id" draggable="true" @dragstart="dragStart(app, $event)"
             class="flex items-center p-3 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md cursor-move hover:border-primary/50 transition">
          <div class="w-9 h-9 rounded-md flex items-center justify-center text-lg flex-shrink-0" :style="{ background: app.color + '20' }">{{ app.icon }}</div>
          <div class="ml-3 flex-1 min-w-0">
            <div class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">{{ app.name }}</div>
            <div class="text-xs text-gray-500 truncate">{{ app.desc }}</div>
          </div>
          <Bars3Icon class="w-4 h-4 text-gray-400" />
        </div>
        <div v-if="availableApps.length === 0" class="text-center py-8 text-sm text-gray-400">
          暂无可添加的应用
        </div>
      </div>
    </div>

    <!-- 右：工作台 -->
    <div class="flex-1 flex flex-col overflow-hidden">
      <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700">
        <h2 class="text-base font-medium text-gray-900 dark:text-gray-100">我的工作台</h2>
        <div class="flex items-center space-x-2 text-sm text-gray-500">
          <span>已添加 <strong class="text-primary font-semibold">{{ workbenchApps.length }}</strong> 个应用</span>
          <button @click="reset" class="text-xs text-primary hover:underline">重置</button>
        </div>
      </div>

      <div class="flex-1 overflow-y-auto p-6 bg-gray-50 dark:bg-[#0E1116]">
        <div class="max-w-6xl mx-auto"
             @dragover.prevent
             @drop="onDrop">
          <div v-if="workbenchApps.length === 0"
               class="border-2 border-dashed border-gray-300 dark:border-gray-700 rounded-xl py-20 text-center">
            <SquaresPlusIcon class="w-12 h-12 mx-auto text-gray-300 dark:text-gray-700" />
            <p class="mt-3 text-sm text-gray-500">从左侧拖拽应用到此处</p>
          </div>

          <div v-else class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-4">
            <div v-for="app in workbenchApps" :key="app.id"
                 class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4 hover:shadow-md hover:border-primary/40 transition relative group">
              <button @click="removeApp(app)" class="absolute top-2 right-2 w-6 h-6 rounded bg-white dark:bg-gray-700 shadow opacity-0 group-hover:opacity-100 hover:bg-red-50 hover:text-red-500 transition flex items-center justify-center">
                <XMarkIcon class="w-3.5 h-3.5" />
              </button>
              <div class="w-12 h-12 rounded-lg flex items-center justify-center text-2xl shadow-sm mb-3" :style="{ background: app.color + '20' }">
                {{ app.icon }}
              </div>
              <div class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">{{ app.name }}</div>
              <div class="text-xs text-gray-500 mt-1 truncate">{{ app.desc }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { mockAppStore } from '@/api/mock'
import { ElMessage } from '@/api/toast'
import { Bars3Icon, SquaresPlusIcon, XMarkIcon } from '@heroicons/vue/24/outline'

const search = ref('')
const allApps = ref(mockAppStore)
const workbenchIds = ref(['im', 'mail', 'doc', 'calendar', 'oa'])

const availableApps = computed(() => {
  const kw = search.value.trim().toLowerCase()
  let list = allApps.value.filter(a => !workbenchIds.value.includes(a.id))
  if (kw) list = list.filter(a => a.name.toLowerCase().includes(kw))
  return list
})

const workbenchApps = computed(() => workbenchIds.value.map(id => allApps.value.find(a => a.id === id)).filter(Boolean))

let draggingApp = null
function dragStart(app, e) {
  draggingApp = app
  e.dataTransfer.effectAllowed = 'copy'
}
function onDrop() {
  if (draggingApp) {
    workbenchIds.value.push(draggingApp.id)
    ElMessage({ message: `「${draggingApp.name}」已添加到工作台`, type: 'success' })
    draggingApp = null
  }
}
function removeApp(app) {
  workbenchIds.value = workbenchIds.value.filter(id => id !== app.id)
  ElMessage({ message: `「${app.name}」已从工作台移除`, type: 'success' })
}
function reset() {
  workbenchIds.value = ['im', 'mail', 'doc', 'calendar', 'oa']
  ElMessage({ message: '已重置为默认工作台', type: 'success' })
}
</script>
