<template>
  <div class="h-full w-full">
    <router-view />
    <ToastHost />
  </div>
</template>

<script setup>
import ToastHost from '@/components/ToastHost.vue'
</script>
