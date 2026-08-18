<template>
  <div class="app-shell h-screen w-screen flex overflow-hidden">
    <Sidebar />
    <div class="app-workspace flex-1 flex flex-col min-w-0">
      <TopBar />
      <main class="workspace-content flex-1 overflow-hidden">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import Sidebar from '@/components/Sidebar.vue'
import TopBar from '@/components/TopBar.vue'
import { useNotificationsStore } from '@/stores/notifications'

const notifStore = useNotificationsStore()

// 每次进入已登录工作区都拉取当前账号的通知，避免侧栏/顶部徽标残留旧账号状态。
onMounted(() => notifStore.load().catch(() => undefined))
</script>

<style scoped>
.app-shell {
  position: relative;
  padding: 10px;
  gap: 10px;
  background:
    radial-gradient(circle at 12% 0%, rgba(79, 135, 255, .18), transparent 30%),
    radial-gradient(circle at 92% 94%, rgba(67, 205, 181, .12), transparent 28%),
    var(--app-bg);
}
.app-workspace {
  min-width: 0;
  overflow: hidden;
  border: 1px solid var(--border-subtle);
  border-radius: 22px;
  background: var(--surface);
  box-shadow: 0 18px 54px rgba(34, 54, 96, .12);
}
.workspace-content { background: var(--surface-soft); }
</style>
