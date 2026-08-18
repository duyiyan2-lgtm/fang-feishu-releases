<template>
  <div class="flex flex-col h-full bg-white dark:bg-gray-900 transition-colors">
    <!-- Banner -->
    <div class="px-8 py-8 bg-gradient-to-r from-primary to-purple-500 text-white flex-shrink-0">
      <h1 class="text-2xl font-semibold">应用中心</h1>
      <p class="text-sm opacity-90 mt-1">发现、安装并管理你的协作应用</p>
      <div class="mt-4 flex items-center space-x-3">
        <div class="bg-white/20 backdrop-blur rounded-md px-3 py-2 text-sm">
          📦 已安装 <strong class="font-semibold">{{ installedCount }}</strong> 个
        </div>
        <div class="bg-white/20 backdrop-blur rounded-md px-3 py-2 text-sm">
          🏪 市场共 <strong class="font-semibold">{{ apps.length }}</strong> 个应用
        </div>
        <router-link to="/app-center/workbench"
                     class="ml-auto bg-white text-primary hover:bg-white/90 px-4 py-2 rounded-md text-sm font-medium transition">
          ⚙️ 配置我的工作台
        </router-link>
      </div>
    </div>

    <!-- 分类 Tab -->
    <div class="px-8 py-3 border-b border-gray-200 dark:border-gray-700 flex items-center space-x-1 flex-shrink-0">
      <button v-for="c in categories" :key="c.value" @click="activeCat = c.value"
              :class="['px-3 h-8 text-sm rounded transition',
                       activeCat === c.value
                         ? 'bg-primary text-white'
                         : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300']">
        {{ c.label }}
        <span v-if="c.value !== 'all'" class="ml-1 text-xs opacity-70">({{ countByCat(c.value) }})</span>
      </button>
    </div>

    <!-- 应用网格 -->
    <div class="flex-1 overflow-y-auto px-8 py-6">
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        <div v-for="app in filteredApps" :key="app.id"
             class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-5 hover:shadow-lg hover:border-primary/40 transition-all">
          <div class="flex items-start mb-3">
            <div class="w-14 h-14 rounded-lg flex items-center justify-center text-2xl shadow-md"
                 :style="{ background: app.color + '20' }">
              {{ app.icon }}
            </div>
            <div class="ml-3 flex-1 min-w-0">
              <h3 class="text-base font-medium text-gray-900 dark:text-gray-100 truncate">{{ app.name }}</h3>
              <div class="text-xs text-gray-500 mt-0.5">{{ app.author }}</div>
              <div class="flex items-center text-xs text-gray-500 mt-1.5">
                <span class="text-yellow-500">★</span>
                <span class="ml-0.5">{{ app.rating }}</span>
                <span class="mx-1.5">·</span>
                <span>{{ formatDownloads(app.downloads) }} 下载</span>
              </div>
            </div>
          </div>
          <p class="text-sm text-gray-600 dark:text-gray-300 h-10 line-clamp-2">{{ app.desc }}</p>
          <button @click="toggleInstall(app)"
                  :class="['mt-4 w-full h-9 rounded-md text-sm font-medium transition',
                           app.installed
                             ? 'border border-gray-200 dark:border-gray-700 text-gray-700 dark:text-gray-300 hover:bg-red-50 hover:text-red-500 hover:border-red-200'
                             : 'bg-primary text-white hover:bg-primary-hover']">
            {{ app.installed ? '已安装（点击卸载）' : '安装' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { mockAppStore } from '@/api/mock'
import { ElMessage } from '@/api/toast'

const apps = ref(JSON.parse(JSON.stringify(mockAppStore)))
const activeCat = ref('all')

const categories = [
  { label: '全部',     value: 'all' },
  { label: '沟通',     value: 'communication' },
  { label: '会议',     value: 'meeting' },
  { label: '协作',     value: 'productivity' },
  { label: '工作流',   value: 'workflow' },
  { label: '业务',     value: 'business' },
  { label: '数据',     value: 'data' }
]

const installedCount = computed(() => apps.value.filter(a => a.installed).length)
const filteredApps = computed(() => activeCat.value === 'all' ? apps.value : apps.value.filter(a => a.category === activeCat.value))
const countByCat = (cat) => apps.value.filter(a => a.category === cat).length

function formatDownloads(n) { return n >= 10000 ? (n / 10000).toFixed(1) + 'w' : n }
function toggleInstall(app) {
  app.installed = !app.installed
  ElMessage({ message: app.installed ? `${app.name} 已安装` : `${app.name} 已卸载`, type: 'success' })
}
</script>
