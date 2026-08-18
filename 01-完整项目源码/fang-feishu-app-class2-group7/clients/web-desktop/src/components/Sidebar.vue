<template>
  <aside
    class="relative flex flex-col h-full flex-shrink-0 bg-sidebar text-white transition-[width] duration-250 ease-smooth shadow-sidebar z-20"
    :class="expanded ? 'w-[220px]' : 'w-[68px]'"
  >
    <!-- Logo -->
    <div class="h-14 flex items-center px-3.5 border-b border-white/[0.06] flex-shrink-0">
      <div class="w-9 h-9 rounded-lg bg-gradient-to-br from-primary to-blue-600 flex items-center justify-center shadow-glow flex-shrink-0">
        <ChatBubbleLeftRightIcon class="w-5 h-5 text-white" />
      </div>
      <transition name="fade">
        <div v-if="expanded" class="ml-2.5 min-w-0 animate-fade-in">
          <div class="text-[13px] font-semibold text-white tracking-wide truncate leading-tight">仿飞书</div>
          <div class="text-[10px] text-white/40 truncate leading-tight mt-0.5">Workspace</div>
        </div>
      </transition>
    </div>

    <!-- 导航 -->
    <nav class="flex-1 py-3 overflow-y-auto scrollbar-auto px-2 space-y-0.5">
      <template v-for="group in menuGroups" :key="group.title">
        <div v-if="expanded" class="px-2.5 pt-3 pb-1.5 first:pt-0">
          <span class="text-[10px] font-medium uppercase tracking-wider text-white/30">{{ group.title }}</span>
        </div>
        <div v-else class="h-2 first:h-0"></div>

        <router-link
          v-for="item in group.items"
          :key="item.path"
          :to="item.path"
          v-slot="{ isActive, navigate }"
          custom
        >
          <a
            role="link"
            :title="expanded ? undefined : item.label"
            class="group relative flex items-center h-10 rounded-lg cursor-pointer select-none
                   transition-all duration-150 ease-smooth outline-none
                   focus-visible:ring-2 focus-visible:ring-primary/50"
            :class="[
              expanded ? 'px-2.5 gap-2.5' : 'justify-center px-0',
              isActive
                ? 'bg-primary text-white shadow-sm'
                : 'text-white/65 hover:bg-white/[0.07] hover:text-white'
            ]"
            @click="navigate"
          >
            <!-- 左侧激活指示条 -->
            <span
              v-if="isActive && !expanded"
              class="absolute left-0 top-1/2 -translate-y-1/2 w-[3px] h-5 rounded-r-full bg-white/90"
            />
            <component
              :is="item.icon"
              class="w-5 h-5 flex-shrink-0 transition-transform duration-150 group-hover:scale-105"
              :class="isActive ? 'text-white' : 'text-white/70 group-hover:text-white'"
            />
            <span v-if="expanded" class="text-[13px] font-medium truncate flex-1">{{ item.label }}</span>
            <span
              v-if="item.badge"
              class="ff-badge"
              :class="expanded ? 'ml-auto' : 'absolute top-1 right-1 scale-90'"
            >{{ formatBadge(item.badge) }}</span>
          </a>
        </router-link>
      </template>
    </nav>

    <!-- 底部：展开切换 + 用户 -->
    <div class="border-t border-white/[0.06] p-2 flex-shrink-0 space-y-1">
      <button
        type="button"
        class="w-full h-9 rounded-lg flex items-center transition-colors duration-150
               text-white/50 hover:text-white hover:bg-white/[0.07]"
        :class="expanded ? 'px-2.5 gap-2.5' : 'justify-center'"
        :title="expanded ? '收起导航' : '展开导航'"
        @click="expanded = !expanded"
      >
        <ChevronDoubleLeftIcon
          class="w-4 h-4 transition-transform duration-250 ease-smooth"
          :class="expanded ? '' : 'rotate-180'"
        />
        <span v-if="expanded" class="text-xs">收起</span>
      </button>

      <div
        class="flex items-center rounded-lg hover:bg-white/[0.07] cursor-default transition-colors"
        :class="expanded ? 'px-2 py-2 gap-2.5' : 'justify-center py-2'"
        :title="displayName"
      >
        <div
          class="w-8 h-8 rounded-full bg-gradient-to-br from-primary to-violet-500
                 flex items-center justify-center text-white text-sm font-semibold flex-shrink-0 ring-2 ring-white/10"
        >
          {{ avatarLetter }}
        </div>
        <div v-if="expanded" class="min-w-0 flex-1 animate-fade-in">
          <div class="text-[13px] text-white font-medium truncate leading-tight">{{ displayName }}</div>
          <div class="flex items-center gap-1 mt-0.5">
            <span
              class="w-1.5 h-1.5 rounded-full flex-shrink-0"
              :class="userStore.isLoggedIn ? 'bg-emerald-400' : 'bg-gray-500'"
            />
            <span class="text-[11px] text-white/40 truncate">{{ onlineText }}</span>
          </div>
        </div>
      </div>
    </div>
  </aside>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { useUserStore } from '@/stores/user'
import { useNotificationsStore } from '@/stores/notifications'
import { useFriendStore } from '@/stores/friend'
import {
  ChatBubbleLeftRightIcon, BellIcon, CalendarIcon, DocumentTextIcon,
  CloudIcon, UserGroupIcon, HeartIcon, UserCircleIcon, ClipboardDocumentCheckIcon,
  Cog6ToothIcon, CheckCircleIcon, BookOpenIcon, ChevronDoubleLeftIcon, HomeIcon
} from '@heroicons/vue/24/outline'

const userStore = useUserStore()
const notifStore = useNotificationsStore()
const friendStore = useFriendStore()

const STORAGE_KEY = 'ff-sidebar-expanded'
const expanded = ref(localStorage.getItem(STORAGE_KEY) !== '0')

watch(expanded, (v) => {
  localStorage.setItem(STORAGE_KEY, v ? '1' : '0')
})

const displayName = computed(() => userStore.displayName)
const avatarLetter = computed(() => (displayName.value || 'U')[0]?.toUpperCase() || 'U')
const onlineText = computed(() => userStore.isLoggedIn ? '在线' : '未登录')

function formatBadge(n) {
  const num = Number(n) || 0
  if (num <= 0) return ''
  return num > 99 ? '99+' : String(num)
}

const menuGroups = computed(() => [
  {
    title: '工作台',
    items: [
      { path: '/home', label: '首页', icon: HomeIcon },
      { path: '/messages', label: '消息', icon: ChatBubbleLeftRightIcon },
      {
        path: '/notifications',
        label: '通知',
        icon: BellIcon,
        badge: notifStore.unreadCount > 0 ? notifStore.unreadCount : 0
      }
    ]
  },
  {
    title: '协作',
    items: [
      { path: '/calendar', label: '日历', icon: CalendarIcon },
      { path: '/documents', label: '文档', icon: DocumentTextIcon },
      { path: '/cloud', label: '云空间', icon: CloudIcon },
      { path: '/contacts', label: '联系人', icon: UserGroupIcon },
      {
        path: '/friends',
        label: '好友',
        icon: HeartIcon,
        badge: friendStore.hasPending ? friendStore.pendingReceived.length : 0
      },
      { path: '/wiki', label: '知识库', icon: BookOpenIcon }
    ]
  },
  {
    title: '流程',
    items: [
      { path: '/approvals', label: '审批', icon: ClipboardDocumentCheckIcon },
      { path: '/tasks', label: '任务', icon: CheckCircleIcon }
    ]
  },
  {
    title: '管理',
    items: [
      { path: '/settings', label: '设置', icon: UserCircleIcon },
      { path: '/admin', label: '管理后台', icon: Cog6ToothIcon }
    ]
  }
])
</script>
