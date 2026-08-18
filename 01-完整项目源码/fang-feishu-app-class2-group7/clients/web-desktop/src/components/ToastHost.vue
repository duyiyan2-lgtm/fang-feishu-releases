<template>
  <div class="fixed top-5 left-1/2 -translate-x-1/2 z-[1000] space-y-2 pointer-events-none">
    <transition-group name="toast">
      <div
        v-for="t in toast.items"
        :key="t.id"
        class="pointer-events-auto px-4 py-2.5 rounded-lg text-sm text-white shadow-float min-w-[220px] max-w-[420px] text-center backdrop-blur-sm border border-white/10"
        :class="
          t.type === 'error' ? 'bg-red-500/95' :
          t.type === 'success' ? 'bg-emerald-500/95' :
          t.type === 'warning' ? 'bg-amber-500/95' : 'bg-gray-800/95'
        "
      >
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
.toast-enter-active,
.toast-leave-active {
  transition: opacity 0.2s cubic-bezier(0.22, 1, 0.36, 1), transform 0.2s cubic-bezier(0.22, 1, 0.36, 1);
}
.toast-enter-from {
  opacity: 0;
  transform: translateY(-8px) scale(0.98);
}
.toast-leave-to {
  opacity: 0;
  transform: translateY(-8px) scale(0.98);
}
</style>
