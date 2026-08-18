<template>
  <Teleport to="body">
    <transition name="dialog" appear>
      <div v-if="modelValue" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
           @click.self="onClose">
        <div class="bg-white dark:bg-gray-900 rounded-xl shadow-2xl w-[440px] p-6">
          <h3 class="text-base font-medium mb-4 dark:text-gray-100">创建会议</h3>
          <p class="text-sm text-gray-500 mb-4">每个账号可创建独立会议，会自动分配声网频道。</p>

          <label class="block text-xs text-gray-500 mb-1.5">会议主题（选填）</label>
          <input v-model="title" placeholder="项目同步会"
                 class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded-md outline-none focus:border-primary focus:ring-2 focus:ring-primary/20" />
          <p class="mt-1 text-xs text-orange-500">⚠️ 后端 schema bug：不传 title 更稳</p>

          <div class="mt-4 flex justify-end space-x-2">
            <button @click="onClose" class="h-8 px-4 text-sm">取消</button>
            <button @click="onCreate" :disabled="submitting"
                    class="h-8 px-4 text-sm bg-primary text-white rounded disabled:opacity-50">
              {{ submitting ? '创建中…' : '立即开始' }}
            </button>
          </div>
        </div>
      </div>
    </transition>
  </Teleport>
</template>

<script setup>
import { ref } from 'vue'
import { useMeetingStore } from '@/stores/meeting'
import { useUserStore } from '@/stores/user'

const props = defineProps({ modelValue: Boolean })
const emit = defineEmits(['update:modelValue', 'created'])

const meetingStore = useMeetingStore()
const userStore = useUserStore()
const title = ref('')
const submitting = ref(false)

async function onCreate() {
  submitting.value = true
  try {
    // 不传 title（后端 schema bug）
    const m = await meetingStore.create({})
    emit('created', m)
    onClose()
  } catch (e) {
    // ElMessage 已在 store 处理
  } finally {
    submitting.value = false
  }
}

function onClose() {
  emit('update:modelValue', false)
}
</script>

<style scoped>
.dialog-enter-active, .dialog-leave-active { transition: opacity 0.2s; }
.dialog-enter-from, .dialog-leave-to { opacity: 0; }
</style>