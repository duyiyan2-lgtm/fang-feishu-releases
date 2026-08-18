<template>
  <div class="flex h-full bg-white dark:bg-gray-900 transition-colors">
    <!-- 左侧菜单 -->
    <div class="w-56 border-r border-gray-200 dark:border-gray-700 py-4 bg-gray-50 dark:bg-[#1A1D23] flex-shrink-0">
      <div class="px-4 mb-3">
        <div class="text-xs text-gray-500 dark:text-gray-400 font-medium px-3">系统管理</div>
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

    <!-- 主内容 -->
    <div class="flex-1 overflow-hidden">
      <router-view v-slot="{ Component }">
        <transition name="fade" mode="out-in">
          <component :is="Component" />
        </transition>
      </router-view>
    </div>
  </div>
</template>

<script setup>
import { useRoute, useRouter } from 'vue-router'
import { UsersIcon, ShieldCheckIcon, BookmarkSquareIcon, ClipboardDocumentListIcon, BuildingOffice2Icon } from '@heroicons/vue/24/outline'

const route = useRoute()
const isActive = (path) => route.path === path || route.path.startsWith(path + '/')

const menus = [
  { path: '/admin/users', label: '用户管理', icon: UsersIcon },
  { path: '/admin/depts', label: '部门管理', icon: BuildingOffice2Icon },
  { path: '/admin/roles', label: '角色权限', icon: ShieldCheckIcon },
  { path: '/admin/dict',  label: '数据字典', icon: BookmarkSquareIcon },
  { path: '/admin/logs',  label: '操作日志', icon: ClipboardDocumentListIcon }
]
</script>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity 0.15s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>
