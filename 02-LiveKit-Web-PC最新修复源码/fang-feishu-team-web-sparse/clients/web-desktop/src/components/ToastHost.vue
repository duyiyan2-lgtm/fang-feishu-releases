<template>
  <div class="fixed top-5 left-1/2 -translate-x-1/2 z-[1000] space-y-2 pointer-events-none">
    <transition-group name="toast">
      <div v-for="t in toast.items" :key="t.id"
           :class="['pointer-events-auto px-4 py-2 rounded-md text-sm text-white shadow-lg min-w-[200px] text-center',
                    t.type === 'error' ? 'bg-red-500' :
                    t.type === 'success' ? 'bg-green-500' :
                    t.type === 'warning' ? 'bg-amber-500' : 'bg-gray-700']">
        {{ t.message }}
      </div>
    </transition-group>
  </div>
</template>

<script setup>
import { useToast } from '@/api/toast'
const toast = useToast()
</script>

<style scoped>
.toast-enter-active, .toast-leave-active { transition: all 0.2s ease; }
.toast-enter-from { opacity: 0; transform: translateY(-10px); }
.toast-leave-to { opacity: 0; transform: translateY(-10px); }
</style>
