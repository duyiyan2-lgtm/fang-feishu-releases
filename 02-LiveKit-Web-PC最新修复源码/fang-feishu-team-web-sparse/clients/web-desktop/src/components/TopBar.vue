<template>
  <header class="topbar flex h-[68px] flex-shrink-0 items-center justify-between px-6">
    <div class="min-w-0">
      <div class="flex items-center gap-2">
        <h1 class="truncate text-[16px] font-bold">{{ pageTitle }}</h1>
        <span class="page-status"><i></i>服务正常</span>
      </div>
      <p class="mt-1 truncate text-[11px]">{{ pageDescription }}</p>
    </div>

    <div class="flex items-center gap-2.5">
      <div class="global-search" :class="{ active: searchOpen }">
        <MagnifyingGlassIcon />
        <input ref="searchInput" v-model="search" @focus="searchOpen = true" @keydown.esc="closeSearch" placeholder="搜索功能或快速跳转" />
        <kbd>Ctrl K</kbd>
        <transition name="topbar-popover">
          <div v-if="searchOpen" v-click-outside="closeSearch" class="search-panel">
            <div class="search-panel__label">快速前往</div>
            <button v-for="item in filteredActions" :key="item.path" @click="goAction(item)">
              <span><component :is="item.icon" /></span>
              <div><strong>{{ item.label }}</strong><small>{{ item.description }}</small></div>
              <kbd>↵</kbd>
            </button>
            <div v-if="!filteredActions.length" class="search-empty">没有匹配的功能</div>
          </div>
        </transition>
      </div>

      <PerformanceIndicator />

      <button @click="themeStore.toggle()" class="icon-button" :title="themeStore.isDark ? '切换到浅色' : '切换到深色'">
        <SunIcon v-if="themeStore.isDark" /><MoonIcon v-else />
      </button>
      <button @click="router.push('/notifications')" class="icon-button relative" title="通知">
        <BellIcon />
        <span v-if="notifStore.unreadCount > 0" class="notification-dot">{{ notifStore.unreadCount > 9 ? '9+' : notifStore.unreadCount }}</span>
      </button>

      <div class="relative">
        <button @click="userMenuOpen = !userMenuOpen" class="top-avatar">
          <img v-if="userStore.userInfo?.avatarUrl" :src="userStore.userInfo.avatarUrl" alt="" />
          <span v-else>{{ avatarLetter }}</span>
        </button>
        <transition name="topbar-popover">
          <div v-if="userMenuOpen" v-click-outside="() => userMenuOpen = false" class="user-menu">
            <div class="user-menu__profile">
              <div class="top-avatar large">{{ avatarLetter }}</div>
              <div class="min-w-0"><strong>{{ userStore.displayName }}</strong><small>{{ userStore.userInfo?.email || '欢迎使用协同工作台' }}</small></div>
            </div>
            <button @click="goProfile"><UserCircleIcon />个人主页</button>
            <button @click="goSettings"><Cog6ToothIcon />账号设置</button>
            <div class="menu-divider"></div>
            <button class="danger" @click="handleLogout"><ArrowRightStartOnRectangleIcon />退出登录</button>
          </div>
        </transition>
      </div>
    </div>
  </header>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { useThemeStore } from '@/stores/theme'
import { useNotificationsStore } from '@/stores/notifications'
import PerformanceIndicator from '@/components/PerformanceIndicator.vue'
import {
  ArrowRightStartOnRectangleIcon, BellIcon, CalendarIcon, ChatBubbleLeftRightIcon,
  CloudIcon, Cog6ToothIcon, DocumentTextIcon, MagnifyingGlassIcon, MoonIcon,
  SunIcon, UserCircleIcon, UserGroupIcon
} from '@heroicons/vue/24/outline'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()
const themeStore = useThemeStore()
const notifStore = useNotificationsStore()
const search = ref('')
const searchOpen = ref(false)
const searchInput = ref(null)
const userMenuOpen = ref(false)

const pageTitle = computed(() => route.meta?.title ?? '协同工作台')
const descriptions = { messages: '实时消息、群聊与视频协作', calendar: '管理你的日程与重要安排', documents: '共同沉淀和编辑团队内容', cloud: '统一管理团队文件与资源', contacts: '快速找到组织内的协作伙伴', settings: '管理个人资料与使用偏好' }
const pageDescription = computed(() => descriptions[String(route.name || '').toLowerCase()] || '让每一次沟通更清晰、更高效')
const avatarLetter = computed(() => (userStore.userInfo?.name || userStore.displayName || 'U')[0].toUpperCase())
const actions = [
  { path: '/messages', label: '消息', description: '进入聊天与视频会议', icon: ChatBubbleLeftRightIcon },
  { path: '/calendar', label: '日历', description: '查看和创建日程', icon: CalendarIcon },
  { path: '/documents', label: '文档', description: '浏览协作文档', icon: DocumentTextIcon },
  { path: '/cloud', label: '云空间', description: '管理团队文件', icon: CloudIcon },
  { path: '/contacts', label: '联系人', description: '搜索组织成员', icon: UserGroupIcon },
  { path: '/settings', label: '账号设置', description: '调整账号与偏好', icon: Cog6ToothIcon }
]
const filteredActions = computed(() => {
  const keyword = search.value.trim().toLowerCase()
  return (keyword ? actions.filter(i => `${i.label}${i.description}`.toLowerCase().includes(keyword)) : actions).slice(0, 6)
})

function goAction(item) { router.push(item.path); closeSearch() }
function closeSearch() { searchOpen.value = false; search.value = '' }
async function handleLogout() {
  userMenuOpen.value = false
  await userStore.logout()
  await router.replace('/login')
}
function goProfile() { userMenuOpen.value = false; router.push('/contacts') }
function goSettings() { userMenuOpen.value = false; router.push('/settings') }
function onShortcut(event) { if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') { event.preventDefault(); searchOpen.value = true; searchInput.value?.focus() } }
onMounted(() => window.addEventListener('keydown', onShortcut))
onBeforeUnmount(() => window.removeEventListener('keydown', onShortcut))

const vClickOutside = {
  mounted(el, binding) { el._outside = (e) => { if (!el.contains(e.target)) binding.value(e) }; setTimeout(() => document.addEventListener('click', el._outside), 0) },
  unmounted(el) { document.removeEventListener('click', el._outside) }
}
</script>

<style scoped>
.topbar { color: var(--text-primary); border-bottom: 1px solid var(--border-subtle); background: var(--surface-elevated); backdrop-filter: blur(18px); }.topbar p { color: var(--text-tertiary); }.page-status { display: inline-flex; align-items: center; gap: 5px; padding: 3px 7px; border-radius: 99px; color: #138664; background: rgba(22,168,121,.09); font-size: 9px; font-weight: 650; }.page-status i { width: 5px; height: 5px; border-radius: 50%; background: #20bb87; box-shadow: 0 0 0 3px rgba(32,187,135,.11); }
.global-search { position: relative; display: flex; align-items: center; width: 248px; height: 38px; border: 1px solid transparent; border-radius: 12px; color: var(--text-tertiary); background: var(--surface-soft); transition: .2s ease; }.global-search.active { border-color: rgba(53,104,244,.35); background: var(--surface); box-shadow: 0 0 0 3px rgba(53,104,244,.08); }.global-search > svg { width: 16px; margin-left: 11px; }.global-search input { width: 100%; min-width: 0; padding: 0 7px; outline: none; color: var(--text-primary); background: transparent; font-size: 11px; }.global-search > kbd { margin-right: 8px; padding: 2px 5px; border: 1px solid var(--border-subtle); border-radius: 5px; color: var(--text-tertiary); background: var(--surface); font-size: 9px; white-space: nowrap; }
.search-panel,.user-menu { position: absolute; top: calc(100% + 10px); right: 0; z-index: 90; padding: 8px; border: 1px solid var(--border-subtle); border-radius: 15px; background: var(--surface-elevated); box-shadow: 0 20px 64px rgba(15,23,42,.18); }.search-panel { left: 0; width: 320px; }.search-panel__label { padding: 5px 8px 7px; color: var(--text-tertiary); font-size: 9px; font-weight: 700; letter-spacing: .12em; }.search-panel button { display: flex; align-items: center; width: 100%; gap: 9px; padding: 9px; border-radius: 10px; text-align: left; transition: .15s; }.search-panel button:hover { background: var(--surface-soft); }.search-panel button > span { display: grid; width: 30px; height: 30px; place-items: center; border-radius: 9px; color: var(--brand); background: var(--brand-soft); }.search-panel button svg { width: 15px; }.search-panel button div { display: flex; flex: 1; flex-direction: column; }.search-panel strong { color: var(--text-primary); font-size: 11px; }.search-panel small { margin-top: 2px; color: var(--text-tertiary); font-size: 9px; }.search-panel button kbd { color: var(--text-tertiary); font-size: 10px; }.search-empty { padding: 22px; color: var(--text-tertiary); font-size: 11px; text-align: center; }
.icon-button { display: grid; width: 38px; height: 38px; place-items: center; border: 1px solid var(--border-subtle); border-radius: 12px; color: var(--text-secondary); background: var(--surface-soft); transition: .18s ease; }.icon-button:hover { color: var(--brand); border-color: rgba(53,104,244,.26); transform: translateY(-1px); }.icon-button svg { width: 17px; }.notification-dot { position: absolute; top: -5px; right: -5px; min-width: 17px; height: 17px; padding: 0 4px; border: 2px solid var(--surface); border-radius: 9px; color: white; background: #ef5b6c; font-size: 8px; line-height: 13px; }
.top-avatar { display: grid; width: 38px; height: 38px; place-items: center; overflow: hidden; border: 3px solid var(--surface); border-radius: 12px; color: white; background: linear-gradient(145deg,#6d5dfc,#3978f6); box-shadow: 0 0 0 1px var(--border-subtle); font-size: 12px; font-weight: 750; }.top-avatar img { width: 100%; height: 100%; object-fit: cover; }.top-avatar.large { flex: 0 0 42px; width: 42px; height: 42px; }
.user-menu { width: 230px; }.user-menu__profile { display: flex; align-items: center; gap: 10px; padding: 8px 7px 12px; border-bottom: 1px solid var(--border-subtle); margin-bottom: 5px; }.user-menu__profile div:last-child { display: flex; flex-direction: column; }.user-menu__profile strong { color: var(--text-primary); font-size: 12px; }.user-menu__profile small { max-width: 145px; margin-top: 2px; overflow: hidden; color: var(--text-tertiary); font-size: 9px; text-overflow: ellipsis; white-space: nowrap; }.user-menu > button { display: flex; align-items: center; width: 100%; height: 35px; gap: 9px; padding: 0 9px; border-radius: 9px; color: var(--text-secondary); font-size: 11px; }.user-menu > button:hover { color: var(--text-primary); background: var(--surface-soft); }.user-menu > button svg { width: 15px; }.user-menu > button.danger { color: #e5485d; }.menu-divider { height: 1px; margin: 5px 0; background: var(--border-subtle); }
.topbar-popover-enter-active,.topbar-popover-leave-active { transition: .15s ease; }.topbar-popover-enter-from,.topbar-popover-leave-to { opacity: 0; transform: translateY(-5px) scale(.98); }
@media (max-width: 1180px) { .global-search { width: 190px; } }
</style>
