<template>
  <Teleport to="body">
    <transition name="drawer" appear>
      <div v-if="visible" class="fixed inset-0 z-50" @click.self="onClose">
        <div class="absolute inset-0 bg-black/30"></div>
        <div class="absolute top-0 right-0 bottom-0 w-[400px] bg-white dark:bg-gray-900 shadow-2xl flex flex-col">
          <!-- 头部 -->
          <div class="h-14 px-5 flex items-center justify-between border-b border-gray-200 dark:border-gray-700">
            <h3 class="text-base font-medium dark:text-gray-100">群详情</h3>
            <button @click="onClose" class="w-8 h-8 rounded hover:bg-gray-100 dark:hover:bg-gray-800 flex items-center justify-center text-gray-500">
              <XMarkIcon class="w-4 h-4" />
            </button>
          </div>

          <!-- 加载 -->
          <div v-if="loading" class="flex-1 flex items-center justify-center text-sm text-gray-400">加载中…</div>

          <!-- 后端无详情 -->
          <div v-else-if="!detail" class="flex-1 flex items-center justify-center p-6 text-center text-sm text-gray-400">
            <div>
              <p>暂无群详情</p>
              <p class="text-xs mt-1">可能你不是该群成员，或后端未返回数据</p>
            </div>
          </div>

          <div v-else class="flex-1 overflow-y-auto">
            <!-- 群信息 -->
            <div class="p-6 text-center border-b border-gray-200 dark:border-gray-700">
              <div class="w-20 h-20 rounded-full bg-primary mx-auto flex items-center justify-center text-white text-2xl font-medium shadow-md">
                {{ detail.title?.[0] || '?' }}
              </div>
              <h4 class="mt-3 text-base font-medium dark:text-gray-100">{{ detail.title || '未命名群' }}</h4>
              <p v-if="detail.notice" class="mt-2 text-xs text-gray-500 italic bg-gray-50 dark:bg-gray-800 rounded p-2">
                📢 {{ detail.notice }}
              </p>
            </div>

            <!-- 操作区（仅 owner 显示） -->
            <section v-if="isOwner" class="px-5 py-3 border-b border-gray-200 dark:border-gray-700 space-y-2">
              <button @click="editNotice" class="w-full h-9 text-sm text-left px-3 hover:bg-gray-50 dark:hover:bg-gray-800 rounded flex items-center">
                ✏️ 修改群公告
              </button>
              <button @click="editTitle" class="w-full h-9 text-sm text-left px-3 hover:bg-gray-50 dark:hover:bg-gray-800 rounded flex items-center">
                ✏️ 修改群名称
              </button>
              <button @click="onDissolve" class="w-full h-9 text-sm text-left px-3 hover:bg-red-50 dark:hover:bg-red-500/10 text-red-600 rounded flex items-center">
                🗑️ 解散群
              </button>
            </section>

            <!-- 成员列表 -->
            <section class="px-5 py-4">
              <div class="flex items-center justify-between mb-3">
                <h5 class="text-sm font-medium text-gray-700 dark:text-gray-300">
                  成员（{{ detail.members.length }}）
                </h5>
                <button @click="showAddMember = true" class="text-xs text-primary hover:underline">
                  + 邀请
                </button>
              </div>
              <div v-for="m in detail.members" :key="m.userId"
                   class="flex items-center py-2">
                <div class="w-8 h-8 rounded-full bg-gray-300 dark:bg-gray-700 flex items-center justify-center text-xs font-medium">
                  {{ m.realName?.[0] || '?' }}
                </div>
                <div class="ml-3 flex-1 min-w-0">
                  <div class="text-sm dark:text-gray-100">{{ m.realName }}</div>
                  <div class="text-xs text-gray-400">
                    {{ m.role === 'Owner' ? '群主' : (m.role === 'Admin' ? '管理员' : '成员') }}
                  </div>
                </div>
                <button v-if="canKick(m)" @click="onKick(m)" class="text-xs text-red-600 hover:underline">
                  踢出
                </button>
              </div>

              <!-- 退群（仅非群主） -->
              <button v-if="!isOwner" @click="onLeave"
                      class="mt-4 w-full h-9 text-sm text-red-600 hover:bg-red-50 dark:hover:bg-red-500/10 rounded">
                退出该群
              </button>
            </section>
          </div>
        </div>
      </div>
    </transition>

    <AddMembersDialog v-if="showAddMember"
                      :conversation-id="conversationId"
                      :existing-member-ids="existingMemberIds"
                      @close="showAddMember = false"
                      @added="onAdded" />
  </Teleport>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { XMarkIcon } from '@heroicons/vue/24/outline'
import { useUserStore } from '@/stores/user'
import { useGroupStore } from '@/stores/group'
import { useMessagesStore } from '@/stores/messages'
import AddMembersDialog from './AddMembersDialog.vue'
import { ElMessage, ElMessageBox } from '@/api/toast'

const props = defineProps({
  conversationId: { type: String, required: true },
  visible: { type: Boolean, default: false }
})
const emit = defineEmits(['update:visible', 'dissolved'])

const userStore = useUserStore()
const groupStore = useGroupStore()
const messagesStore = useMessagesStore()

const showAddMember = ref(false)

const loading = computed(() => groupStore.isLoading(props.conversationId))
const detail = computed(() => groupStore.details[props.conversationId])

const isOwner = computed(() =>
  detail.value && userStore.userInfo && detail.value.ownerId === userStore.userInfo.id
)

const canKick = (m) => {
  if (!detail.value || !userStore.userInfo) return false
  if (m.userId === userStore.userInfo.id) return false
  return isOwner.value || m.role !== 'Owner'
}

const existingMemberIds = computed(() =>
  (detail.value?.members || []).map(m => m.userId)
)

watch(() => props.visible, (v) => {
  if (v && props.conversationId) groupStore.fetchDetail(props.conversationId)
}, { immediate: true })

async function editTitle() {
  try {
    const { value } = await ElMessageBox.prompt('请输入新的群名称', '修改群名', {
      confirmButtonText: '保存', cancelButtonText: '取消',
      inputValue: detail.value.title || ''
    })
    if (value && value !== detail.value.title) {
      await groupStore.update(props.conversationId, { title: value })
    }
  } catch (e) {
    if (e !== 'cancel' && e !== 'close') ElMessage({ message: '修改失败：' + e.message, type: 'error' })
  }
}

async function editNotice() {
  try {
    const { value } = await ElMessageBox.prompt('请输入群公告', '修改群公告', {
      confirmButtonText: '保存', cancelButtonText: '取消',
      inputType: 'textarea', inputValue: detail.value.notice || ''
    })
    if (value !== (detail.value.notice || '')) {
      await groupStore.update(props.conversationId, { notice: value })
    }
  } catch (e) {
    if (e !== 'cancel' && e !== 'close') ElMessage({ message: '修改失败：' + e.message, type: 'error' })
  }
}

async function onKick(m) {
  try {
    await ElMessageBox.confirm(`确定踢出 ${m.realName}？`, '踢出成员', { type: 'warning' })
    await groupStore.removeMemberApi(props.conversationId, m.userId)
  } catch (e) {
    if (e !== 'cancel' && e !== 'close') ElMessage({ message: '踢人失败：' + e.message, type: 'error' })
  }
}

async function onLeave() {
  try {
    await ElMessageBox.confirm('确定退出该群？', '退出群聊', { type: 'warning' })
    const myId = userStore.userInfo.id
    await groupStore.removeMemberApi(props.conversationId, myId)
    messagesStore.removeLocalConversation(props.conversationId)
    onClose()
  } catch (e) {
    if (e !== 'cancel' && e !== 'close') ElMessage({ message: '退出失败：' + e.message, type: 'error' })
  }
}

async function onDissolve() {
  try {
    await ElMessageBox.confirm('解散后无法恢复，确定解散？', '解散群',
      { type: 'warning', confirmButtonText: '解散' })
    await groupStore.dissolve(props.conversationId)
    messagesStore.removeLocalConversation(props.conversationId)
    emit('dissolved')
    onClose()
  } catch (e) {
    if (e !== 'cancel' && e !== 'close') ElMessage({ message: '解散失败：' + e.message, type: 'error' })
  }
}

async function onAdded(userIds) {
  showAddMember.value = false
  await groupStore.addMembers(props.conversationId, userIds)
}

function onClose() {
  emit('update:visible', false)
}
</script>

<style scoped>
.drawer-enter-active, .drawer-leave-active { transition: transform 0.25s; }
.drawer-enter-from, .drawer-leave-to { transform: translateX(100%); }
</style>
