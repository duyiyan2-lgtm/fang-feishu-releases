<template>
  <div v-if="show" class="absolute bottom-12 left-4 z-20 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-xl w-64 max-h-64 overflow-y-auto">
    <div class="px-3 py-1.5 text-xs text-gray-400 border-b border-gray-100 dark:border-gray-700">
      选择成员
    </div>
    <div v-for="m in filteredMembers" :key="m.userId"
         @click="$emit('pick', m)"
         class="flex items-center px-3 py-2 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700 text-sm dark:text-gray-100">
      <div class="w-6 h-6 rounded-full bg-primary flex items-center justify-center text-white text-xs">
        {{ m.realName?.[0] }}
      </div>
      <span class="ml-2 flex-1">{{ m.realName }}</span>
    </div>
    <div v-if="filteredMembers.length === 0" class="px-3 py-3 text-xs text-gray-400 text-center">没有匹配成员</div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  show: Boolean,
  keyword: { type: String, default: '' },
  members: { type: Array, default: () => [] }
})
defineEmits(['pick'])

const filteredMembers = computed(() => {
  const kw = props.keyword.toLowerCase()
  if (!kw) return props.members.slice(0, 20)
  return props.members
    .filter(m => (m.realName || '').toLowerCase().includes(kw))
    .slice(0, 20)
})
</script>
