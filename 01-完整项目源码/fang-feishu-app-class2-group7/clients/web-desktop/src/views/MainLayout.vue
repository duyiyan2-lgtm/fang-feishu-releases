<template>
  <div class="h-screen w-screen flex flex-col bg-bg dark:bg-[#0E1116] overflow-hidden transition-colors duration-200">
    <!-- Electron 自定义标题栏 -->
    <TitleBar />

    <div class="flex-1 flex min-h-0 overflow-hidden">
      <Sidebar />
      <div class="flex-1 flex flex-col min-w-0 min-h-0">
        <TopBar />
        <main class="flex-1 overflow-hidden relative bg-bg dark:bg-[#0E1116]">
          <!-- keep-alive：切换模块不丢状态，Web 端更流畅 -->
          <router-view v-slot="{ Component, route }">
            <keep-alive :include="keepAliveNames" :max="8">
              <component
                :is="Component"
                :key="route.meta?.keepAliveKey || route.name"
                class="h-full w-full"
              />
            </keep-alive>
          </router-view>
        </main>
      </div>
    </div>
  </div>
</template>

<script setup>
import Sidebar from '@/components/Sidebar.vue'
import TopBar from '@/components/TopBar.vue'
import TitleBar from '@/components/TitleBar.vue'

/** 高频模块缓存，减少重复请求与白屏闪烁 */
const keepAliveNames = [
  'Home',
  'Messages',
  'Notifications',
  'Calendar',
  'Documents',
  'Cloud',
  'Contacts',
  'Friends',
  'Wiki',
  'Tasks',
  'ApprovalList',
  'Settings'
]
</script>
