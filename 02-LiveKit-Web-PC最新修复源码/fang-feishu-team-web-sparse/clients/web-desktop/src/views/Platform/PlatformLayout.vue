<template>
  <div class="flex h-full bg-white dark:bg-gray-900 transition-colors">
    <div class="w-56 border-r border-gray-200 dark:border-gray-700 py-4 bg-gray-50 dark:bg-[#1A1D23] flex-shrink-0">
      <div class="px-4 mb-3">
        <div class="text-xs text-gray-500 dark:text-gray-400 font-medium px-3">开放平台</div>
      </div>
      <nav class="space-y-0.5">
        <a v-for="item in menus" :key="item.path" @click="$router.push(item.path)"
           :class="['mx-3 flex items-center px-3 h-9 rounded-md cursor-pointer transition text-sm',
                    isActive(item.path)
                      ? 'bg-primary text-white shadow-sm'
                      : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800']">
          <component :is="item.icon" class="w-[18px] h-[18px] mr-3 flex-shrink-0" />
          <span>{{ item.label }}</span>
        </a>
      </nav>
    </div>

    <div class="flex-1 overflow-hidden">
      <router-view />
    </div>
  </div>
</template>

<script setup>
import { useRoute } from 'vue-router'
import { CodeBracketIcon, BoltIcon, KeyIcon } from '@heroicons/vue/24/outline'

const route = useRoute()
const isActive = (path) => route.path === path || route.path.startsWith(path + '/')

const menus = [
  { path: '/platform/api',     label: 'API 文档',   icon: CodeBracketIcon },
  { path: '/platform/webhook', label: 'Webhook',    icon: BoltIcon },
  { path: '/platform/apps',    label: '应用鉴权',   icon: KeyIcon }
]
</script>
