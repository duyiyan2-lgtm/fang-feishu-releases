<template>
  <Teleport to="body">
    <div class="fixed inset-0 z-[60] flex items-center justify-center bg-black/40" @click.self="$emit('close')">
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-2xl w-[440px] max-h-[80vh] flex flex-col">
        <div class="h-12 px-5 flex items-center justify-between border-b border-gray-200 dark:border-gray-700">
          <h3 class="text-sm font-medium dark:text-gray-100">邀请成员</h3>
          <button @click="$emit('close')" class="w-8 h-8 rounded hover:bg-gray-100 dark:hover:bg-gray-800 flex items-center justify-center text-gray-500">
            <XMarkIcon class="w-4 h-4" />
          </button>
        </div>
        <div class="p-3">
          <input v-model="search" placeholder="搜索联系人"
                 class="w-full h-8 px-3 text-sm bg-gray-100 dark:bg-gray-800 dark:text-gray-100 rounded-md outline-none focus:bg-white dark:focus:bg-gray-900 focus:ring-2 focus:ring-primary/20" />
        </div>
        <div class="flex-1 overflow-y-auto">
          <div v-if="loading" class="p-8 text-center text-sm text-gray-400">加载中…</div>
          <div v-else-if="filteredContacts.length === 0" class="p-6 text-center text-sm text-gray-400">没有可邀请的联系人</div>
          <div v-for="c in filteredContacts" :key="c.id"
               @click="toggle(c)"
               class="px-5 py-2 flex items-center cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800"
               :class="isSelected(c.id) ? 'bg-primary-50 dark:bg-primary/20' : ''">
            <div class="w-8 h-8 rounded-full bg-primary flex items-center justify-center text-white text-xs font-medium">
              {{ c.realName?.[0] }}
            </div>
            <div class="ml-3 flex-1 min-w-0">
              <div class="text-sm font-medium dark:text-gray-100">{{ c.realName }}</div>
              <div class="text-xs text-gray-500 truncate">{{ c.department }}</div>
            </div>
            <div v-if="isSelected(c.id)" class="text-primary">✓</div>
          </div>
        </div>
        <div class="px-5 py-3 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between">
          <span class="text-sm text-gray-500">已选 {{ selectedIds.length }} 人</span>
          <div>
            <button @click="$emit('close')" class="h-8 px-4 text-sm mr-2">取消</button>
            <button @click="onConfirm" :disabled="selectedIds.length === 0"
                    class="h-8 px-4 text-sm bg-primary hover:bg-primary-hover text-white rounded disabled:opacity-50">
              邀请
            </button>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { listContacts, adaptContact } from '@/api/contacts'
import { XMarkIcon } from '@heroicons/vue/24/outline'

const props = defineProps({
  conversationId: String,
  existingMemberIds: { type: Array, default: () => [] }
})
const emit = defineEmits(['close', 'added'])

const allContacts = ref([])
const selectedIds = ref([])
const search = ref('')
const loading = ref(false)

const filteredContacts = computed(() => {
  const set = new Set(props.existingMemberIds)
  let list = allContacts.value.filter(c => !set.has(c.id))
  const kw = search.value.trim().toLowerCase()
  if (kw) list = list.filter(c => (c.realName || '').toLowerCase().includes(kw))
  return list
})

const isSelected = (id) => selectedIds.value.includes(id)

function toggle(c) {
  const i = selectedIds.value.indexOf(c.id)
  if (i >= 0) selectedIds.value.splice(i, 1)
  else selectedIds.value.push(c.id)
}

function onConfirm() {
  emit('added', selectedIds.value)
}

onMounted(async () => {
  if (allContacts.value.length > 0) return
  loading.value = true
  try {
    const list = await listContacts()
    allContacts.value = (list || []).map(adaptContact)
  } catch (e) {
    console.error('[AddMembersDialog] loadContacts failed:', e)
  } finally {
    loading.value = false
  }
})
</script>
