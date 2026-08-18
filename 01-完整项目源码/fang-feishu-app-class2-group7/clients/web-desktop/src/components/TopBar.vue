<template>
  <header
    class="h-13 bg-white/90 dark:bg-gray-900/90 backdrop-blur-md border-b border-line dark:border-gray-700/80
           flex items-center justify-between px-5 flex-shrink-0 transition-colors duration-200 z-10"
  >
    <div class="flex items-center gap-3 min-w-0">
      <h1 class="text-base font-semibold text-ink dark:text-gray-100 truncate tracking-tight">
        {{ pageTitle }}
      </h1>
      <span
        v-if="pageHint"
        class="hidden md:inline text-2xs text-ink-tertiary dark:text-gray-500 px-2 py-0.5 rounded-full bg-surface-tertiary dark:bg-gray-800"
      >{{ pageHint }}</span>
    </div>

    <div class="flex items-center gap-1.5">
      <!-- 全局搜索 -->
      <div class="relative hidden sm:block">
        <MagnifyingGlassIcon class="absolute left-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-ink-tertiary pointer-events-none" />
        <input
          v-model="search"
          type="search"
          placeholder="搜索消息、文档、联系人…"
          class="w-52 lg:w-64 h-8 pl-9 pr-3 text-sm rounded-md outline-none
                 bg-surface-tertiary dark:bg-gray-800 text-ink dark:text-gray-100
                 placeholder:text-ink-tertiary border border-transparent
                 hover:bg-line-soft dark:hover:bg-gray-700
                 focus:w-72 focus:bg-white dark:focus:bg-gray-900
                 focus:border-primary/40 focus:ring-2 focus:ring-primary/15
                 transition-all duration-200 ease-smooth"
          @keydown.enter.prevent="onSearch"
        />
      </div>

      <button
        type="button"
        class="ff-icon-btn"
        :title="themeStore.isDark ? '切换到浅色模式' : '切换到深色模式'"
        @click="themeStore.toggle()"
      >
        <SunIcon v-if="themeStore.isDark" class="w-4 h-4 text-amber-300" />
        <MoonIcon v-else class="w-4 h-4" />
      </button>

      <button
        type="button"
        class="ff-icon-btn relative"
        title="通知中心"
        @click="$router.push('/notifications')"
      >
        <BellIcon class="w-4 h-4" />
        <span
          v-if="notifStore.unreadCount > 0"
          class="absolute top-0.5 right-0.5 ff-badge scale-90"
        >{{ notifStore.unreadCount > 99 ? '99+' : notifStore.unreadCount }}</span>
      </button>

      <!-- 用户菜单 -->
      <div class="relative ml-1">
        <button
          type="button"
          class="w-8 h-8 rounded-full bg-gradient-to-br from-primary to-violet-500
                 flex items-center justify-center text-white text-sm font-semibold
                 ring-2 ring-transparent hover:ring-primary/30 transition-all duration-150 active:scale-95"
          @click="userMenuOpen = !userMenuOpen"
        >
          {{ avatarLetter }}
        </button>

        <transition name="menu">
          <div
            v-if="userMenuOpen"
            v-click-outside="() => (userMenuOpen = false)"
            class="absolute right-0 top-10 w-60 bg-white dark:bg-gray-800 rounded-xl shadow-float
                   border border-line dark:border-gray-700 py-1.5 z-50 origin-top-right"
          >
            <div class="px-4 py-3 border-b border-line-soft dark:border-gray-700">
              <div class="text-sm font-semibold text-ink dark:text-gray-100">{{ userStore.displayName }}</div>
              <div class="text-xs text-ink-tertiary mt-0.5 truncate">{{ userStore.userInfo?.email || '未绑定邮箱' }}</div>
            </div>
            <button
              type="button"
              class="w-full text-left px-4 py-2 text-sm text-ink dark:text-gray-200 hover:bg-surface-secondary dark:hover:bg-gray-700 transition-colors"
              @click="goProfile"
            >个人主页</button>
            <button
              type="button"
              class="w-full text-left px-4 py-2 text-sm text-ink dark:text-gray-200 hover:bg-surface-secondary dark:hover:bg-gray-700 transition-colors"
              @click="goSettings"
            >账号设置</button>
            <div class="border-t border-line-soft dark:border-gray-700 my-1" />
            <button
              type="button"
              class="w-full text-left px-4 py-2 text-sm text-red-500 hover:bg-red-50 dark:hover:bg-red-500/10 transition-colors"
              @click="handleLogout"
            >退出登录</button>
          </div>
        </transition>
      </div>
    </div>
  </header>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useThemeStore } from '@/stores/theme'
import { useNotificationsStore } from '@/stores/notifications'
import { MagnifyingGlassIcon, BellIcon, SunIcon, MoonIcon } from '@heroicons/vue/24/outline'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const themeStore = useThemeStore()
const notifStore = useNotificationsStore()

const search = ref('')
const userMenuOpen = ref(false)

const pageTitle = computed(() => route.meta?.title ?? '工作台')
const pageHint = computed(() => {
  const map = {
    Home: '工作台概览',
    Messages: '即时沟通',
    Notifications: '系统与业务通知',
    Calendar: '日程协作',
    Documents: '在线文档',
    Cloud: '文件云空间',
    Contacts: '组织通讯录',
    Friends: '好友关系',
    Wiki: '知识沉淀',
    Tasks: '任务跟踪',
    ApprovalList: '流程审批',
    Settings: '账号与偏好'
  }
  return map[route.name] || ''
})
const avatarLetter = computed(() => (userStore.userInfo?.name || userStore.displayName || 'U')[0]?.toUpperCase() || 'U')

function handleLogout() {
  userStore.logout()
  userMenuOpen.value = false
  router.push('/login')
}

function goProfile() {
  userMenuOpen.value = false
  router.push('/contacts')
}

function goSettings() {
  userMenuOpen.value = false
  router.push('/settings')
}

function onSearch() {
  const q = search.value.trim()
  if (!q) return
  // 优先进入消息搜索语境
  router.push({ path: '/messages', query: { q } })
}

const vClickOutside = {
  mounted(el, binding) {
    el._h = (e) => {
      if (!el.contains(e.target)) binding.value(e)
    }
    setTimeout(() => document.addEventListener('click', el._h), 0)
  },
  unmounted(el) {
    document.removeEventListener('click', el._h)
  }
}
</script>
