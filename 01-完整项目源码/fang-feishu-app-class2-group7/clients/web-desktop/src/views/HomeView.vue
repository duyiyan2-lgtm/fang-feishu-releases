<template>
  <div class="h-full overflow-y-auto scrollbar-auto bg-bg dark:bg-[#0E1116]">
    <div class="max-w-6xl mx-auto px-6 py-6 space-y-6 animate-slide-up">
      <!-- 欢迎区 -->
      <section
        class="relative overflow-hidden rounded-2xl bg-gradient-to-br from-[#1A3A8F] via-[#2B5CDE] to-[#3370FF]
               text-white p-6 sm:p-8 shadow-card"
      >
        <div class="absolute inset-0 opacity-25 pointer-events-none"
             style="background: radial-gradient(circle at 85% 20%, #fff 0%, transparent 40%), radial-gradient(circle at 10% 90%, #a5b4fc 0%, transparent 35%);" />
        <div class="relative flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
          <div>
            <p class="text-sm text-white/70">{{ greeting }} · {{ todayLabel }}</p>
            <h1 class="mt-1 text-2xl sm:text-3xl font-bold tracking-tight">
              {{ displayName }}，欢迎回到工作台
            </h1>
            <p class="mt-2 text-sm text-white/75 max-w-xl">
              消息、日历、云盘、审批一站入口。从下方快捷方式开始今天的协作。
            </p>
          </div>
          <div class="flex gap-2 flex-shrink-0">
            <button type="button" class="h-9 px-4 rounded-lg bg-white text-primary text-sm font-medium hover:bg-white/95 active:scale-[0.98] transition" @click="$router.push('/messages')">
              打开消息
            </button>
            <button type="button" class="h-9 px-4 rounded-lg bg-white/15 border border-white/25 text-sm font-medium hover:bg-white/25 active:scale-[0.98] transition" @click="$router.push('/approvals/new')">
              发起审批
            </button>
          </div>
        </div>
      </section>

      <!-- 统计卡片 -->
      <section class="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4">
        <div
          v-for="card in statCards"
          :key="card.label"
          class="ff-card p-4 hover:shadow-card transition-shadow cursor-pointer group"
          @click="$router.push(card.to)"
        >
          <div class="flex items-start justify-between">
            <div
              class="w-10 h-10 rounded-xl flex items-center justify-center transition-transform group-hover:scale-105"
              :class="card.bg"
            >
              <component :is="card.icon" class="w-5 h-5" :class="card.iconClass" />
            </div>
            <span v-if="card.hint" class="text-2xs text-ink-tertiary">{{ card.hint }}</span>
          </div>
          <div class="mt-3 text-2xl font-bold text-ink dark:text-gray-100 tabular-nums">{{ card.value }}</div>
          <div class="text-sm text-ink-secondary dark:text-gray-400 mt-0.5">{{ card.label }}</div>
        </div>
      </section>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <!-- 快捷入口 -->
        <section class="lg:col-span-2 ff-card p-5">
          <div class="flex items-center justify-between mb-4">
            <h2 class="text-base font-semibold text-ink dark:text-gray-100">快捷入口</h2>
            <span class="text-2xs text-ink-tertiary">常用模块</span>
          </div>
          <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-3">
            <button
              v-for="app in apps"
              :key="app.path"
              type="button"
              class="flex flex-col items-center gap-2 p-4 rounded-xl border border-line-soft dark:border-gray-700/80
                     bg-surface-secondary/50 dark:bg-gray-800/40
                     hover:border-primary/40 hover:bg-primary-soft dark:hover:bg-primary/10
                     hover:shadow-soft active:scale-[0.98] transition-all"
              @click="$router.push(app.path)"
            >
              <div class="w-11 h-11 rounded-xl flex items-center justify-center" :class="app.bg">
                <component :is="app.icon" class="w-5 h-5" :class="app.iconClass" />
              </div>
              <span class="text-sm font-medium text-ink dark:text-gray-100">{{ app.label }}</span>
              <span class="text-2xs text-ink-tertiary text-center leading-tight">{{ app.desc }}</span>
            </button>
          </div>
        </section>

        <!-- 今日提示 -->
        <section class="ff-card p-5 flex flex-col">
          <h2 class="text-base font-semibold text-ink dark:text-gray-100 mb-4">今日提示</h2>
          <ul class="space-y-3 flex-1">
            <li
              v-for="tip in tips"
              :key="tip.title"
              class="flex gap-3 p-3 rounded-lg bg-surface-secondary/80 dark:bg-gray-800/50 border border-transparent hover:border-line dark:hover:border-gray-700 transition-colors"
            >
              <span class="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0 text-sm" :class="tip.bg">{{ tip.emoji }}</span>
              <div class="min-w-0">
                <div class="text-sm font-medium text-ink dark:text-gray-100">{{ tip.title }}</div>
                <div class="text-xs text-ink-tertiary mt-0.5 leading-relaxed">{{ tip.body }}</div>
              </div>
            </li>
          </ul>
          <button
            type="button"
            class="mt-4 ff-btn-secondary w-full"
            @click="$router.push('/calendar')"
          >
            查看日历
          </button>
        </section>
      </div>

      <!-- 最近动态占位 -->
      <section class="ff-card p-5">
        <div class="flex items-center justify-between mb-3">
          <h2 class="text-base font-semibold text-ink dark:text-gray-100">工作概览</h2>
        </div>
        <div class="grid sm:grid-cols-3 gap-3 text-sm">
          <div class="rounded-xl p-4 bg-primary-soft dark:bg-primary/10 border border-primary/10">
            <div class="text-ink-secondary dark:text-gray-400 text-xs mb-1">沟通</div>
            <div class="font-medium text-ink dark:text-gray-100">消息实时同步 · SignalR</div>
          </div>
          <div class="rounded-xl p-4 bg-emerald-50 dark:bg-emerald-500/10 border border-emerald-500/10">
            <div class="text-ink-secondary dark:text-gray-400 text-xs mb-1">协作</div>
            <div class="font-medium text-ink dark:text-gray-100">文档 / 云盘 / 知识库</div>
          </div>
          <div class="rounded-xl p-4 bg-violet-50 dark:bg-violet-500/10 border border-violet-500/10">
            <div class="text-ink-secondary dark:text-gray-400 text-xs mb-1">流程</div>
            <div class="font-medium text-ink dark:text-gray-100">审批流 · 任务跟踪</div>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup>
defineOptions({ name: 'Home' })

import { computed, onMounted, ref } from 'vue'
import { useUserStore } from '@/stores/user'
import { useNotificationsStore } from '@/stores/notifications'
import { useFriendStore } from '@/stores/friend'
import { useMessagesStore } from '@/stores/messages'
import {
  ChatBubbleLeftRightIcon,
  BellIcon,
  CalendarIcon,
  DocumentTextIcon,
  CloudIcon,
  ClipboardDocumentCheckIcon,
  CheckCircleIcon,
  UserGroupIcon,
  BookOpenIcon
} from '@heroicons/vue/24/outline'

const userStore = useUserStore()
const notifStore = useNotificationsStore()
const friendStore = useFriendStore()
const messagesStore = useMessagesStore()

const unreadConv = ref(0)

const displayName = computed(() => userStore.displayName || '同事')
const greeting = computed(() => {
  const h = new Date().getHours()
  if (h < 6) return '夜深了'
  if (h < 11) return '早上好'
  if (h < 14) return '中午好'
  if (h < 18) return '下午好'
  return '晚上好'
})
const todayLabel = computed(() => {
  const d = new Date()
  const w = ['日', '一', '二', '三', '四', '五', '六'][d.getDay()]
  return `${d.getMonth() + 1}月${d.getDate()}日 周${w}`
})

const statCards = computed(() => [
  {
    label: '未读会话',
    value: unreadConv.value,
    to: '/messages',
    hint: '消息',
    icon: ChatBubbleLeftRightIcon,
    bg: 'bg-primary-soft dark:bg-primary/15',
    iconClass: 'text-primary'
  },
  {
    label: '系统通知',
    value: notifStore.unreadCount || 0,
    to: '/notifications',
    hint: '通知',
    icon: BellIcon,
    bg: 'bg-amber-50 dark:bg-amber-500/15',
    iconClass: 'text-amber-600 dark:text-amber-400'
  },
  {
    label: '好友申请',
    value: friendStore.pendingReceived?.length || 0,
    to: '/friends',
    hint: '好友',
    icon: UserGroupIcon,
    bg: 'bg-pink-50 dark:bg-pink-500/15',
    iconClass: 'text-pink-600 dark:text-pink-400'
  },
  {
    label: '待办审批',
    value: '→',
    to: '/approvals',
    hint: '流程',
    icon: ClipboardDocumentCheckIcon,
    bg: 'bg-violet-50 dark:bg-violet-500/15',
    iconClass: 'text-violet-600 dark:text-violet-400'
  }
])

const apps = [
  { path: '/messages', label: '消息', desc: '即时沟通', icon: ChatBubbleLeftRightIcon, bg: 'bg-primary/10', iconClass: 'text-primary' },
  { path: '/calendar', label: '日历', desc: '日程安排', icon: CalendarIcon, bg: 'bg-sky-500/10', iconClass: 'text-sky-600' },
  { path: '/cloud', label: '云盘', desc: '文件空间', icon: CloudIcon, bg: 'bg-cyan-500/10', iconClass: 'text-cyan-600' },
  { path: '/approvals', label: '审批', desc: '流程中心', icon: ClipboardDocumentCheckIcon, bg: 'bg-violet-500/10', iconClass: 'text-violet-600' },
  { path: '/documents', label: '文档', desc: '在线协作', icon: DocumentTextIcon, bg: 'bg-emerald-500/10', iconClass: 'text-emerald-600' },
  { path: '/tasks', label: '任务', desc: '事项跟踪', icon: CheckCircleIcon, bg: 'bg-orange-500/10', iconClass: 'text-orange-600' },
  { path: '/contacts', label: '通讯录', desc: '组织架构', icon: UserGroupIcon, bg: 'bg-indigo-500/10', iconClass: 'text-indigo-600' },
  { path: '/wiki', label: '知识库', desc: '知识沉淀', icon: BookOpenIcon, bg: 'bg-rose-500/10', iconClass: 'text-rose-600' }
]

const tips = [
  { emoji: '📅', title: '查看今日日程', body: '打开日历确认会议与提醒，避免冲突。', bg: 'bg-sky-50 dark:bg-sky-500/15' },
  { emoji: '✅', title: '处理待办审批', body: '优先处理「待我审批」，减少流程阻塞。', bg: 'bg-violet-50 dark:bg-violet-500/15' },
  { emoji: '💬', title: '回复重要消息', body: '未读会话集中在消息列表顶部。', bg: 'bg-primary-soft dark:bg-primary/15' }
]

onMounted(async () => {
  try {
    if (!messagesStore.conversations?.length) {
      await messagesStore.fetchConversations()
    }
    unreadConv.value = (messagesStore.conversations || []).filter(c => (c.unread || 0) > 0).length
  } catch {
    unreadConv.value = 0
  }
  try {
    await notifStore.load()
  } catch { /* optional */ }
})
</script>
