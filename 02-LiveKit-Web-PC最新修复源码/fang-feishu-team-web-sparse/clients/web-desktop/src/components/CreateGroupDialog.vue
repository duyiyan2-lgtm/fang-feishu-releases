<template>
  <Teleport to="body">
    <transition name="dialog" appear>
      <div v-if="modelValue" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
           @click.self="onClose">
        <div class="bg-white dark:bg-gray-900 rounded-xl shadow-2xl w-[500px] max-h-[80vh] flex flex-col">
          <!-- 头部 -->
          <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700">
            <h3 class="text-base font-medium dark:text-gray-100">创建群聊</h3>
            <button @click="onClose" class="w-8 h-8 rounded hover:bg-gray-100 dark:hover:bg-gray-800 flex items-center justify-center text-gray-500">
              <XMarkIcon class="w-4 h-4" />
            </button>
          </div>

          <!-- 群名输入 -->
          <div class="px-6 py-3 border-b border-gray-200 dark:border-gray-700">
            <label class="block text-xs text-gray-500 dark:text-gray-400 mb-1.5">群名称（选填）</label>
            <input v-model="form.title" placeholder="可不填，创建后修改"
                   class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded-md outline-none focus:border-primary focus:ring-2 focus:ring-primary/20" />
            <p class="mt-1 text-xs text-orange-500">
              ⚠️ 当前后端 schema 不接受 title，建群后会暂时显示「未知群」
            </p>
          </div>

          <!-- 搜索 + 联系人列表 -->
          <div class="flex-1 flex flex-col min-h-0">
            <div class="p-3 border-b border-gray-200 dark:border-gray-700">
              <input v-model="form.search" placeholder="搜索联系人"
                     class="w-full h-8 px-3 text-sm bg-gray-100 dark:bg-gray-800 dark:text-gray-100 rounded-md outline-none focus:bg-white dark:focus:bg-gray-900 focus:ring-2 focus:ring-primary/20" />
            </div>
            <div class="flex-1 overflow-y-auto">
              <div v-if="loading" class="p-8 text-center text-sm text-gray-400">
                <svg class="animate-spin w-5 h-5 mx-auto mb-2" viewBox="0 0 24 24" fill="none">
                  <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
                  <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
                </svg>
                加载中…
              </div>
              <div v-else-if="filteredContacts.length === 0" class="p-8 text-center text-sm text-gray-400">
                没有找到联系人
              </div>
              <div v-for="c in filteredContacts" :key="c.id"
                   @click="toggle(c)"
                   class="px-6 py-2 flex items-center cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800 transition"
                   :class="isSelected(c.id) ? 'bg-primary-50 dark:bg-primary/20' : ''">
                <div class="w-8 h-8 rounded-full flex items-center justify-center text-white text-sm font-medium flex-shrink-0"
                     :style="{ background: c.color || '#3370FF' }">
                  {{ c.realName?.[0] || '?' }}
                </div>
                <div class="ml-3 flex-1 min-w-0">
                  <div class="text-sm font-medium dark:text-gray-100">{{ c.realName }}</div>
                  <div class="text-xs text-gray-500 truncate">{{ c.department }}</div>
                </div>
                <div v-if="isSelected(c.id)" class="w-5 h-5 rounded-full bg-primary text-white flex items-center justify-center text-xs">✓</div>
              </div>
            </div>
          </div>

          <!-- 已选 + 操作 -->
          <div class="px-6 py-3 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between">
            <span class="text-sm text-gray-500">
              已选 <b class="text-primary">{{ selectedIds.length }}</b> 人
              <span v-if="selectedIds.length > 0 && selectedIds.length < 2" class="ml-2 text-xs text-orange-500">
                （群人数需至少 2 人）
              </span>
            </span>
            <div class="flex items-center space-x-2">
              <button @click="onClose" class="h-8 px-4 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-md">
                取消
              </button>
              <button @click="onCreate" :disabled="!canCreate || submitting"
                      class="h-8 px-4 text-sm bg-primary hover:bg-primary-hover text-white rounded-md transition disabled:opacity-50 disabled:cursor-not-allowed">
                {{ submitting ? '创建中…' : '创建' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </transition>
  </Teleport>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'
import { listContacts, adaptContact } from '@/api/contacts'
import { createConversation } from '@/api/im'
import { useUserStore } from '@/stores/user'
import { XMarkIcon } from '@heroicons/vue/24/outline'

const props = defineProps({ modelValue: Boolean })
const emit = defineEmits(['update:modelValue', 'created'])

const userStore = useUserStore()

const form = reactive({ title: '', search: '' })
const allContacts = ref([])
const selectedIds = ref([])
const submitting = ref(false)
const loading = ref(false)

const filteredContacts = computed(() => {
  const kw = form.search.trim().toLowerCase()
  // 过滤掉自己
  let list = allContacts.value.filter(c => c.id !== userStore.userInfo?.id)
  if (kw) list = list.filter(c => (c.realName || '').toLowerCase().includes(kw))
  return list
})

const canCreate = computed(() => selectedIds.value.length >= 2)

function isSelected(id) { return selectedIds.value.includes(id) }
function toggle(c) {
  const i = selectedIds.value.indexOf(c.id)
  if (i >= 0) selectedIds.value.splice(i, 1)
  else selectedIds.value.push(c.id)
}

async function loadContacts() {
  if (allContacts.value.length > 0) return
  loading.value = true
  try {
    const list = await listContacts()
    allContacts.value = (list || []).map(adaptContact)
  } catch (e) {
    console.error('[CreateGroupDialog] loadContacts failed:', e)
  } finally {
    loading.value = false
  }
}

async function onCreate() {
  if (!canCreate.value) return
  submitting.value = true
  try {
    // 后端 Group 不接受 title 字段（OPEN-Q），所以不传
    const conv = await createConversation({
      type: 'Group',
      memberUserIds: selectedIds.value
    })
    const convId = conv.id
    emit('created', convId)
    onClose()
    // 重置
    form.title = ''
    form.search = ''
    selectedIds.value = []
  } catch (e) {
    // ElMessage 已在 http.js 拦截器中处理
  } finally {
    submitting.value = false
  }
}

function onClose() {
  emit('update:modelValue', false)
}

watch(() => props.modelValue, (v) => { if (v) loadContacts() })
</script>

<style scoped>
.dialog-enter-active, .dialog-leave-active { transition: opacity 0.2s; }
.dialog-enter-from, .dialog-leave-to { opacity: 0; }
</style>
