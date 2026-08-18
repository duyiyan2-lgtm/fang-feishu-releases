<template>
  <div class="flex h-full bg-white dark:bg-gray-900 transition-colors">
    <div class="flex-1 flex flex-col overflow-hidden">
      <!-- 顶部 -->
      <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700 flex-shrink-0">
        <h2 class="text-base font-medium dark:text-gray-100">
          我的好友 <span v-if="!loading" class="text-sm text-gray-400 ml-1">({{ friendsList.length }})</span>
        </h2>
        <button @click="showAddFriend = true"
                class="h-8 px-4 text-sm bg-primary hover:bg-primary-hover text-white rounded-md flex items-center">
          <UserPlusIcon class="w-4 h-4 mr-1" />
          添加
        </button>
      </div>

      <!-- 内容 -->
      <div class="flex-1 overflow-y-auto">
        <!-- 加载中 -->
        <div v-if="loading" class="p-8 text-center text-sm text-gray-400">
          <svg class="animate-spin w-5 h-5 mx-auto mb-2" viewBox="0 0 24 24" fill="none">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
            <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
          </svg>
          加载中…
        </div>

        <!-- 空状态 -->
        <div v-else-if="friendsList.length === 0" class="p-12 text-center text-gray-400">
          <UserGroupIcon class="w-16 h-16 mx-auto mb-3 opacity-50" />
          <p class="text-sm">还没有好友</p>
          <button @click="showAddFriend = true"
                  class="mt-4 h-8 px-4 text-sm bg-primary text-white rounded-md">
            + 添加好友
          </button>
        </div>

        <!-- 好友列表 -->
        <div v-else>
          <div v-for="f in friendsList" :key="f.id"
               class="px-6 py-3 flex items-center hover:bg-gray-50 dark:hover:bg-gray-800 transition group">
            <div class="w-10 h-10 rounded-full bg-primary flex items-center justify-center text-white font-medium">
              {{ f.realName?.[0] || '?' }}
            </div>
            <div class="ml-3 flex-1 min-w-0">
              <div class="text-sm font-medium dark:text-gray-100">{{ f.realName || '?' }}</div>
              <div class="text-xs text-gray-500 truncate">
                {{ f.departmentName || f.department || '' }} · {{ f.username }}
              </div>
            </div>
            <button @click="startChatWith(f.id)"
                    class="h-8 px-3 text-sm bg-primary/10 hover:bg-primary/20 text-primary rounded flex items-center transition"
                    title="发起私聊">
              <ChatBubbleLeftRightIcon class="w-4 h-4 mr-1" />
              私聊
            </button>
            <button @click="onRemove(f.id, f.realName)"
                    class="ml-2 h-8 px-3 text-sm bg-red-50 dark:bg-red-500/10 hover:bg-red-100 text-red-600 rounded flex items-center transition"
                    title="删除好友">
              删除
            </button>
          </div>
        </div>
      </div>
    </div>

    <AddFriendDialog v-model="showAddFriend" @added="onAdded" />
    <FriendRequestsToast />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { UserPlusIcon, UserGroupIcon, ChatBubbleLeftRightIcon } from '@heroicons/vue/24/outline'
import { useFriendStore } from '@/stores/friend'
import { useUserStore } from '@/stores/user'
import { ElMessage, ElMessageBox } from '@/api/toast'
import AddFriendDialog from '@/components/AddFriendDialog.vue'
import FriendRequestsToast from '@/components/FriendRequestsToast.vue'

const router = useRouter()
const friendStore = useFriendStore()
const userStore = useUserStore()

const showAddFriend = ref(false)
const loading = computed(() => friendStore.loading)

const friendsList = computed(() => {
  return Object.values(friendStore.friends || {})
})

onMounted(() => {
  friendStore.fetchAll()
})

function onAdded() {
  showAddFriend.value = false
  friendStore.fetchAll()
}

async function onRemove(userId, realName) {
  try {
    await ElMessageBox.confirm(`确定删除好友 ${realName}？`, '删除好友', { type: 'warning' })
  } catch { return }
  await friendStore.remove(userId)
}

async function startChatWith(peerId) {
  if (!peerId) return
  // 跳到 messages 路由并选中该用户
  await router.push({ path: '/messages', query: { to: peerId } })
}
</script>