<template>
  <Teleport to="body">
    <transition-group name="slide-down" tag="div" class="fixed top-20 left-1/2 -translate-x-1/2 z-[70] space-y-2">
      <div v-for="req in friendStore.pendingReceived" :key="req.id"
           class="bg-white dark:bg-gray-800 rounded-xl shadow-2xl border border-gray-200 dark:border-gray-700 px-5 py-3 flex items-center space-x-3 min-w-[360px]">
        <div class="w-10 h-10 rounded-full bg-primary flex items-center justify-center text-white font-medium flex-shrink-0">
          {{ req.requester?.realName?.[0] || '?' }}
        </div>
        <div class="flex-1 min-w-0">
          <div class="text-sm font-medium text-gray-900 dark:text-gray-100">
            {{ req.requester?.realName || '?' }} 申请添加你为好友
          </div>
          <div class="text-xs text-gray-500 mt-0.5">来自 {{ req.requester?.departmentName || req.requester?.username || '?' }}</div>
        </div>
        <button @click="friendStore.rejectRequest(req.id)"
                class="h-8 px-3 text-sm bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 rounded">
          拒绝
        </button>
        <button @click="friendStore.acceptRequest(req.id)"
                class="h-8 px-3 text-sm bg-primary hover:bg-primary-hover text-white rounded">
          接受
        </button>
      </div>
    </transition-group>
  </Teleport>
</template>

<script setup>
import { onMounted, onUnmounted } from 'vue'
import { useFriendStore } from '@/stores/friend'

const friendStore = useFriendStore()

onMounted(() => {
  friendStore.fetchAll()
})
</script>

<style scoped>
.slide-down-enter-active, .slide-down-leave-active { transition: all 0.3s; }
.slide-down-enter-from, .slide-down-leave-to {
  transform: translate(-50%, -200%);
  opacity: 0;
}
</style>