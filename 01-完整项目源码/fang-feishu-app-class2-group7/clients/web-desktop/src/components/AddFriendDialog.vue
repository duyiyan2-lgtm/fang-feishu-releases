<template>
  <Teleport to="body">
    <transition name="dialog" appear>
      <div v-if="modelValue" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
           @click.self="onClose">
        <div class="bg-white dark:bg-gray-900 rounded-xl shadow-2xl w-[460px] max-h-[80vh] flex flex-col">
          <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700">
            <h3 class="text-base font-medium dark:text-gray-100">添加好友</h3>
            <button @click="onClose" class="w-8 h-8 rounded hover:bg-gray-100 dark:hover:bg-gray-800 flex items-center justify-center text-gray-500">
              <XMarkIcon class="w-4 h-4" />
            </button>
          </div>

          <!-- 搜索框 -->
          <div class="p-4 border-b border-gray-200 dark:border-gray-700">
            <div class="relative">
              <MagnifyingGlassIcon class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input v-model="search" placeholder="搜索用户名 / 姓名 / 拼音"
                     class="w-full h-9 pl-9 pr-3 text-sm bg-gray-100 dark:bg-gray-800 dark:text-gray-100 rounded-md outline-none focus:bg-white dark:focus:bg-gray-900 focus:ring-2 focus:ring-primary/20" />
            </div>
          </div>

          <!-- 搜索结果 -->
          <div class="flex-1 overflow-y-auto">
            <div v-if="search.trim().length === 0" class="p-8 text-center text-sm text-gray-400">
              输入姓名搜索联系人
            </div>
            <div v-else-if="searching" class="p-8 text-center text-sm text-gray-400">
              <svg class="animate-spin w-5 h-5 mx-auto mb-2" viewBox="0 0 24 24" fill="none">
                <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
                <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
              </svg>
              搜索中…
            </div>
            <div v-else-if="results.length === 0" class="p-8 text-center text-sm text-gray-400">
              没有找到联系人
            </div>
            <div v-for="c in results" :key="c.id"
                 class="px-6 py-3 flex items-center hover:bg-gray-50 dark:hover:bg-gray-800 transition"
                 :class="{ 'opacity-50': isSelf(c) || isFriend(c) }">
              <div class="w-10 h-10 rounded-full bg-primary flex items-center justify-center text-white font-medium">
                {{ c.realName?.[0] || c.username?.[0] || '?' }}
              </div>
              <div class="ml-3 flex-1 min-w-0">
                <div class="text-sm font-medium dark:text-gray-100">{{ c.realName || c.username }}</div>
                <div class="text-xs text-gray-500 truncate">{{ c.departmentName || c.department || '' }} · {{ c.username }}</div>
              </div>
              <span v-if="isSelf(c)" class="text-xs text-gray-400">自己</span>
              <span v-else-if="isFriend(c)" class="text-xs text-green-600">已是好友</span>
              <button v-else @click="onSend(c)" :disabled="sending === c.id"
                      class="h-8 px-3 text-sm bg-primary hover:bg-primary-hover text-white rounded disabled:opacity-50">
                {{ sending === c.id ? '发送中…' : '发送请求' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </transition>
  </Teleport>
</template>

<script setup>
import { ref, watch, computed } from 'vue'
import { XMarkIcon, MagnifyingGlassIcon } from '@heroicons/vue/24/outline'
import { listContacts, searchContacts, adaptContact } from '@/api/contacts'
import { useUserStore } from '@/stores/user'
import { useFriendStore } from '@/stores/friend'

const props = defineProps({ modelValue: Boolean })
const emit = defineEmits(['update:modelValue', 'added'])

const userStore = useUserStore()
const friendStore = useFriendStore()

const search = ref('')
const allContacts = ref([])
const searching = ref(false)
const sending = ref(null)
let searchTimer = null

const results = computed(() => {
  const kw = search.value.trim().toLowerCase()
  if (!kw) return []
  return allContacts.value.filter(c =>
    (c.realName || '').toLowerCase().includes(kw) ||
    (c.username || '').toLowerCase().includes(kw) ||
    (c.pinyin || '').toLowerCase().includes(kw)
  ).slice(0, 20)
})

function isSelf(c) { return c.id === userStore.userInfo?.id }
function isFriend(c) { return friendStore.statusOf(c.id) === 'friend' }

async function loadContacts() {
  if (allContacts.value.length > 0) return
  searching.value = true
  try {
    // 后端 [FromQuery] string keyword 必填且非空，搜空/空格都 400
    // 用 '_' 绕过：[FromQuery] 默认是 Contains，返所有非好友
    const list = await searchContacts('_').catch(() => [])
    allContacts.value = (Array.isArray(list) ? list : []).map(adaptContact)
  } catch (e) {
    console.error('[AddFriendDialog] load failed', e)
  } finally {
    searching.value = false
  }
}

async function onSend(c) {
  sending.value = c.id
  try {
    const r = await friendStore.sendRequest(c.id)
    if (r) emit('added', r)
  } finally {
    sending.value = null
  }
}

function onClose() {
  emit('update:modelValue', false)
  search.value = ''
}

watch(() => props.modelValue, (v) => {
  if (v) {
    loadContacts()
    friendStore.fetchAll()
  }
})
</script>

<style scoped>
.dialog-enter-active, .dialog-leave-active { transition: opacity 0.2s; }
.dialog-enter-from, .dialog-leave-to { opacity: 0; }
</style>