<template>
  <aside class="sidebar-shell flex h-full w-[248px] flex-shrink-0 flex-col text-slate-300">
    <div class="brand-row">
      <div class="brand-mark">
        <span></span><span></span><span></span><span></span>
      </div>
      <div class="min-w-0">
        <div class="truncate text-[15px] font-bold tracking-wide text-white">仿飞书协同</div>
        <div class="mt-0.5 truncate text-[10px] font-medium tracking-[.14em] text-blue-200/60">FANG WORKSPACE</div>
      </div>
    </div>

    <div class="px-3 pb-3">
      <button class="quick-create" @click="$router.push('/messages')">
        <PlusIcon class="h-4 w-4" />
        <span>发起协作</span>
        <kbd>Ctrl K</kbd>
      </button>
    </div>

    <nav class="sidebar-nav flex-1 overflow-y-auto px-3 pb-4">
      <section v-for="group in menuGroups" :key="group.title" class="menu-section">
        <h3>{{ group.title }}</h3>
        <router-link v-for="item in group.items" :key="item.path" :to="item.path" custom v-slot="{ isActive, navigate }">
          <a @click="navigate" class="menu-link" :class="{ active: isActive }">
            <span class="menu-icon"><component :is="item.icon" /></span>
            <span class="min-w-0 flex-1 truncate">{{ item.label }}</span>
            <span v-if="item.badge" class="menu-badge">{{ item.badge > 99 ? '99+' : item.badge }}</span>
            <span v-else-if="isActive" class="active-dot"></span>
          </a>
        </router-link>
      </section>
    </nav>

    <div class="user-card" @click="$router.push('/settings')">
      <div class="user-avatar">
        <img v-if="userStore.userInfo?.avatarUrl" :src="userStore.userInfo.avatarUrl" alt="" />
        <span v-else>{{ avatarLetter }}</span>
        <i></i>
      </div>
      <div class="min-w-0 flex-1">
        <div class="truncate text-[13px] font-semibold text-white">{{ displayName }}</div>
        <div class="mt-0.5 flex items-center gap-1.5 text-[11px] text-slate-400">
          <span>{{ onlineText }}</span><span class="text-slate-600">·</span><span>个人工作区</span>
        </div>
      </div>
      <ChevronRightIcon class="h-4 w-4 text-slate-500" />
    </div>
  </aside>
</template>

<script setup>
import { computed } from 'vue'
import { useUserStore } from '@/stores/user'
import { useNotificationsStore } from '@/stores/notifications'
import { useFriendStore } from '@/stores/friend'
import {
  BellIcon, BookOpenIcon, CalendarIcon, ChatBubbleLeftRightIcon, CheckCircleIcon,
  ChevronRightIcon, ClipboardDocumentCheckIcon, CloudIcon, Cog6ToothIcon,
  DocumentTextIcon, HeartIcon, PlusIcon, UserCircleIcon, UserGroupIcon
} from '@heroicons/vue/24/outline'

const userStore = useUserStore()
const notifStore = useNotificationsStore()
const friendStore = useFriendStore()
const displayName = computed(() => userStore.displayName)
const avatarLetter = computed(() => (displayName.value || 'U')[0].toUpperCase())
const onlineText = computed(() => userStore.isLoggedIn ? '在线' : '未登录')

const menuGroups = computed(() => [
  { title: '沟通', items: [
    { path: '/messages', label: '消息', icon: ChatBubbleLeftRightIcon },
    { path: '/notifications', label: '消息通知', icon: BellIcon, badge: notifStore.unreadCount }
  ] },
  { title: '协作空间', items: [
    { path: '/calendar', label: '日历', icon: CalendarIcon },
    { path: '/documents', label: '文档', icon: DocumentTextIcon },
    { path: '/cloud', label: '云空间', icon: CloudIcon },
    { path: '/contacts', label: '联系人', icon: UserGroupIcon },
    { path: '/friends', label: '好友', icon: HeartIcon, badge: friendStore.hasPending ? friendStore.pendingReceived.length : 0 },
    { path: '/wiki', label: '知识库', icon: BookOpenIcon }
  ] },
  { title: '效率工具', items: [
    { path: '/approvals', label: '审批', icon: ClipboardDocumentCheckIcon },
    { path: '/tasks', label: '任务', icon: CheckCircleIcon }
  ] },
  { title: '管理', items: [
    { path: '/settings', label: '账号设置', icon: UserCircleIcon },
    { path: '/admin', label: '管理后台', icon: Cog6ToothIcon }
  ] }
])
</script>

<style scoped>
.sidebar-shell { position: relative; overflow: hidden; border: 1px solid rgba(255,255,255,.08); border-radius: 22px; background: linear-gradient(165deg, #121e35 0%, #0b1324 55%, #10172a 100%); box-shadow: 0 18px 50px rgba(9, 18, 39, .22); }
.sidebar-shell::before { content: ''; position: absolute; inset: 0 0 auto; height: 210px; pointer-events: none; background: radial-gradient(circle at 18% 10%, rgba(75, 126, 255, .26), transparent 54%); }
.brand-row { position: relative; display: flex; align-items: center; gap: 11px; height: 72px; padding: 0 18px; }
.brand-mark { display: grid; grid-template-columns: repeat(2, 10px); gap: 4px; width: 34px; height: 34px; padding: 5px; border-radius: 11px; background: linear-gradient(145deg, #4a7cff, #2854dc); box-shadow: 0 8px 22px rgba(53, 104, 244, .35); }
.brand-mark span { border-radius: 3px; background: rgba(255,255,255,.94); }.brand-mark span:last-child { background: #72e3c6; }
.quick-create { position: relative; z-index: 1; display: flex; align-items: center; width: 100%; height: 38px; gap: 9px; padding: 0 11px; border: 1px solid rgba(147, 177, 255, .18); border-radius: 11px; color: #dfe9ff; background: rgba(83, 126, 238, .12); font-size: 12px; font-weight: 600; transition: .2s ease; }
.quick-create:hover { border-color: rgba(147, 177, 255, .32); background: rgba(83, 126, 238, .2); transform: translateY(-1px); }.quick-create kbd { margin-left: auto; padding: 2px 5px; border: 1px solid rgba(255,255,255,.12); border-radius: 5px; color: #7f91b1; font-size: 9px; font-weight: 500; }
.sidebar-nav { position: relative; }.sidebar-nav::-webkit-scrollbar { width: 0; }.menu-section { margin-top: 13px; }.menu-section:first-child { margin-top: 0; }.menu-section h3 { margin: 0 9px 6px; color: #657492; font-size: 10px; font-weight: 700; letter-spacing: .13em; text-transform: uppercase; }
.menu-link { display: flex; align-items: center; height: 38px; margin: 2px 0; padding: 0 10px; border-radius: 11px; color: #aab6cc; cursor: pointer; font-size: 12px; font-weight: 520; transition: .18s ease; }.menu-link:hover { color: #f5f8ff; background: rgba(255,255,255,.055); transform: translateX(2px); }.menu-link.active { color: white; background: linear-gradient(90deg, rgba(69, 112, 236, .42), rgba(69, 112, 236, .16)); box-shadow: inset 0 0 0 1px rgba(135, 164, 255, .11); }
.menu-icon { display: grid; width: 28px; place-items: center; }.menu-icon :deep(svg) { width: 17px; height: 17px; }.menu-link.active .menu-icon { color: #83a7ff; }.active-dot { width: 5px; height: 5px; border-radius: 50%; background: #79e4c7; box-shadow: 0 0 0 4px rgba(121,228,199,.1); }.menu-badge { min-width: 19px; height: 18px; padding: 0 5px; border-radius: 9px; color: white; background: #ef5b6c; font-size: 9px; line-height: 18px; text-align: center; }
.user-card { position: relative; display: flex; align-items: center; gap: 10px; margin: 0 10px 10px; padding: 10px; border: 1px solid rgba(255,255,255,.07); border-radius: 14px; background: rgba(255,255,255,.035); cursor: pointer; transition: .2s ease; }.user-card:hover { border-color: rgba(125,160,255,.22); background: rgba(255,255,255,.065); }.user-avatar { position: relative; display: grid; flex: 0 0 34px; width: 34px; height: 34px; place-items: center; overflow: visible; border-radius: 11px; color: white; background: linear-gradient(145deg, #6d5dfc, #3c74f5); font-size: 13px; font-weight: 700; }.user-avatar img { width: 100%; height: 100%; border-radius: inherit; object-fit: cover; }.user-avatar i { position: absolute; right: -2px; bottom: -2px; width: 9px; height: 9px; border: 2px solid #10182a; border-radius: 50%; background: #35c58b; }
</style>
