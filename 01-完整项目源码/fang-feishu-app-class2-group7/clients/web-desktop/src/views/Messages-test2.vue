<template>
  <div class="flex h-full bg-white dark:bg-gray-900 transition-colors">
    <!-- 左：会话列表 -->
    <div class="w-80 border-r border-gray-200 dark:border-gray-700 flex flex-col bg-white dark:bg-gray-900">
      <div class="h-14 px-5 flex items-center justify-between border-b border-gray-200 dark:border-gray-700">
        <h2 class="text-base font-medium text-gray-800 dark:text-gray-100 flex items-center gap-2">
          消息
          <span :class="['w-2 h-2 rounded-full', connected ? 'bg-green-500' : 'bg-gray-300']"
                :title="connected ? 'SignalR 已连接' : '未连接'"></span>
        </h2>
        <div class="flex items-center space-x-1">
          <button @click="showCreateGroup = true" class="w-8 h-8 rounded-md hover:bg-gray-100 dark:hover:bg-gray-800 flex items-center justify-center" title="发起群聊">
            <UserPlusIcon class="w-4 h-4 text-gray-600 dark:text-gray-300" />
          </button>
        </div>
      </div>

      <div class="p-3 border-b border-gray-200 dark:border-gray-700">
        <div class="relative">
          <MagnifyingGlassIcon class="absolute left-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <input v-model="search" placeholder="搜索"
                 class="w-full h-8 pl-9 pr-3 text-sm bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 focus:bg-white dark:focus:bg-gray-900 focus:ring-2 focus:ring-primary/30 border border-transparent focus:border-primary/40 rounded-md outline-none transition-all dark:text-gray-100" />
        </div>
      </div>

      <div class="flex-1 overflow-y-auto">
        <div v-if="loading && conversations.length === 0" class="p-8 text-center text-sm text-gray-400">
          <svg class="animate-spin w-5 h-5 mx-auto mb-2" viewBox="0 0 24 24" fill="none">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
            <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
          </svg>
          加载中…
        </div>
        <div v-else-if="filteredConvs.length === 0" class="p-8 text-center text-sm text-gray-400">没有找到会话</div>

        <div v-for="conv in filteredConvs" :key="conv.id"
             @click="selectConv(conv.id)"
             class="px-3 py-3 cursor-pointer border-l-2 transition-colors"
             :class="messagesStore.activeId === conv.id
               ? 'bg-primary-50 dark:bg-primary/20 border-primary'
               : 'border-transparent hover:bg-gray-50 dark:hover:bg-gray-800'">
          <div class="flex">
            <div class="relative flex-shrink-0">
              <div class="w-10 h-10 rounded-md flex items-center justify-center text-white font-medium"
                   :style="{ background: peerColor(conv) }">{{ conv.name?.[0] || '?' }}</div>
              <span v-if="conv.unread" class="absolute -top-1 -right-1 bg-red-500 text-white text-[10px] rounded-full min-w-[18px] h-[18px] flex items-center justify-center px-1 font-medium">{{ conv.unread }}</span>
            </div>
            <div class="ml-3 flex-1 min-w-0">
              <div class="flex justify-between items-baseline">
                <span class="font-medium text-sm text-gray-900 dark:text-gray-100 truncate flex items-center">
                  {{ conv.name }}
                  <UserGroupIcon v-if="conv.type === 'group'" class="w-3 h-3 ml-1 text-gray-400" />
                </span>
                <span class="text-xs text-gray-400 ml-2 flex-shrink-0">{{ conv.lastTime }}</span>
              </div>
              <p class="mt-1 text-sm text-gray-500 dark:text-gray-400 truncate">
                <span v-if="conv.lastIsRecalled" class="italic">消息已撤回</span>
                <span v-else>
                  <span v-if="conv.lastSender" class="text-gray-400">{{ conv.lastSender }}: </span>{{ conv.lastMessage }}
                </span>
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 右：聊天窗口 -->
    <div class="flex-1 flex flex-col bg-gray-50 dark:bg-[#1A1D23]">
      <template v-if="activeConv">
        <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900">
          <div class="flex items-center">
            <div class="w-9 h-9 rounded-md flex items-center justify-center text-white font-medium"
                 :style="{ background: peerColor(activeConv) }">{{ activeConv.name?.[0] || '?' }}</div>
            <div class="ml-3">
              <div class="font-medium text-sm text-gray-900 dark:text-gray-100">{{ activeConv.name }}</div>
              <div class="text-xs text-gray-500 flex items-center">
                <span :class="['inline-block w-2 h-2 rounded-full mr-1', connected ? 'bg-green-500' : 'bg-gray-400']"></span>
                {{ connected ? '在线' : '离线' }}
              </div>
            </div>
          </div>
          <div class="flex items-center space-x-1">
            <button v-if="activeConv?.type === 'group'" @click="showGroupPanel = true"
                    class="w-8 h-8 rounded-md hover:bg-gray-100 dark:hover:bg-gray-800 flex items-center justify-center" title="群详情">
              <UserGroupIcon class="w-4 h-4 text-gray-600 dark:text-gray-300" />
            </button>
            <button v-if="activeConv?.type === 'group'" @click="onGroupVideoCall"
                    class="h-8 px-3 rounded-md hover:opacity-90 flex items-center bg-gradient-to-br from-primary to-purple-500 text-white"
                    title="群会议（Agora 多人视频）">
              <VideoCameraIcon class="w-4 h-4" />
              <span class="ml-1 text-xs font-medium">群会议</span>
            </button>
            <button @click="showMeetingCenter = true"
                    class="w-8 h-8 rounded-md hover:bg-gray-100 dark:hover:bg-gray-800 flex items-center justify-center"
                    title="会议中心（Agora 声网）">
              <VideoCameraIcon class="w-4 h-4 text-gray-600 dark:text-gray-300" />
            </button>
            <button class="w-8 h-8 rounded-md hover:bg-gray-100 dark:hover:bg-gray-800 flex items-center justify-center"><EllipsisHorizontalIcon class="w-4 h-4 text-gray-600 dark:text-gray-300" /></button>
          </div>
        </div>

        <div ref="messageList" class="flex-1 overflow-y-auto px-6 py-6 relative">
          <div class="max-w-3xl mx-auto space-y-3">
            <div v-for="msg in activeMessages" :key="msg.id"
                 :class="['flex', msg.sender === 'me' ? 'justify-end' : 'justify-start']">
              <div v-if="msg.sender !== 'me'" class="w-8 h-8 rounded-md flex-shrink-0 flex items-center justify-center text-white text-xs font-medium mr-2"
                   :style="{ background: peerColor(activeConv) }">{{ activeConv.name?.[0] }}</div>

              <div class="group relative max-w-md" :class="msg.sender === 'me' ? 'ml-2 mr-1' : 'ml-2'">
                <div v-if="msg.sender !== 'me' && msg.senderName" class="text-xs text-gray-500 ml-1 mb-1">{{ msg.senderName }}</div>
                <div :class="['px-4 py-2 rounded-lg text-sm shadow-sm',
                              msg.recalled
                                ? 'bg-gray-100 dark:bg-gray-800 text-gray-400 italic'
                                : msg.sender === 'me'
                                  ? 'bg-primary text-white'
                                  : 'bg-white dark:bg-gray-700 text-gray-800 dark:text-gray-100']">
                  <div v-if="msg.recalled" class="flex items-center text-xs">
                    <ArrowUturnLeftIcon class="w-3 h-3 mr-1" />
                    {{ msg.sender === 'me' ? '你' : msg.senderName }} 撤回了一条消息
                  </div>
                  <div v-else class="whitespace-pre-wrap break-words" v-html="renderContent(msg.content)"></div>
                  <div :class="['text-[10px] mt-1 text-right flex items-center justify-end gap-1',
                                msg.recalled ? 'text-gray-400' : msg.sender === 'me' ? 'text-white/70' : 'text-gray-400']">
                    <span v-if="msg.pending" class="italic">发送中…</span>
                    <span>{{ msg.time }}</span>
                    <!-- 已读回执：自己发的消息才显示 -->
                    <span v-if="msg.sender === 'me' && !msg.pending && !msg.recalled && (msg.readCount || 0) >= 1"
                          @mouseenter="loadReaders(msg)"
                          class="cursor-pointer hover:opacity-80">
                      已读 {{ msg.readCount }}
                    </span>
                  </div>
                </div>

                <button v-if="msg.sender === 'me' && !msg.recalled"
                        @click.stop="activeRecall = msg.id"
                        class="absolute -top-2 right-1 w-5 h-5 bg-white dark:bg-gray-700 rounded-full shadow opacity-0 group-hover:opacity-100 transition flex items-center justify-center text-gray-500 hover:text-primary">
                  <EllipsisHorizontalIcon class="w-3 h-3" />
                </button>
              </div>

              <div v-if="msg.sender === 'me'" class="w-8 h-8 rounded-md flex-shrink-0 flex items-center justify-center text-white text-xs font-medium bg-gradient-to-br from-primary to-purple-500 ml-2">{{ userInitial }}</div>
            </div>
          </div>
        </div>

        <!-- 撤回操作菜单 -->
        <transition
          enter-active-class="transition duration-100"
          enter-from-class="opacity-0 scale-95"
          enter-to-class="opacity-100 scale-100">
          <div v-if="activeRecall" class="absolute bottom-20 right-12 w-32 bg-white dark:bg-gray-800 rounded-md shadow-xl border border-gray-200 dark:border-gray-700 py-1 z-30">
            <button @click="doRecall(activeRecall)" class="block w-full text-left px-3 py-1.5 text-sm text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-700 flex items-center">
              <ArrowUturnLeftIcon class="w-3.5 h-3.5 mr-2" />撤回
            </button>
            <button @click="activeRecall = null" class="block w-full text-left px-3 py-1.5 text-sm text-gray-500 hover:bg-gray-50 dark:hover:bg-gray-700">取消</button>
          </div>
        </transition>

        <div class="border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 px-6 py-3">
          <div class="max-w-3xl mx-auto">
            <div class="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden focus-within:border-primary transition-colors">
              <textarea v-model="inputText" @input="onInput" @keydown.enter.exact.prevent="send" rows="3"
                        placeholder="输入消息，回车发送，Shift+回车换行；输入 @ 选择成员"
                        class="w-full px-3 py-2 text-sm bg-white dark:bg-gray-900 outline-none resize-none text-gray-800 dark:text-gray-100 placeholder-gray-400" />

              <AtMembersPopover :show="atShow"
                                 :keyword="atKeyword"
                                 :members="groupMembers"
                                 @pick="insertAt" />
              <div class="flex items-center justify-between px-2 py-1.5 border-t border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50 relative">
                <div class="flex items-center space-x-1">
                  <div class="relative">
                    <button @click="showEmoji = !showEmoji" class="w-7 h-7 rounded hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center justify-center">
                      <FaceSmileIcon class="w-4 h-4 text-gray-500" />
                    </button>
                    <div v-if="showEmoji" v-click-outside="() => showEmoji = false"
                         class="absolute bottom-9 left-0 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-2 shadow-xl w-72 grid grid-cols-8 gap-1 z-20">
                      <button v-for="e in emojis" :key="e" @click="insertEmoji(e)" class="w-8 h-8 rounded hover:bg-gray-100 dark:hover:bg-gray-700 text-xl flex items-center justify-center">{{ e }}</button>
                    </div>
                  </div>
                </div>
                <button @click="send" :disabled="!inputText.trim()"
                        class="px-4 h-7 bg-primary hover:bg-primary-hover disabled:bg-gray-300 disabled:cursor-not-allowed text-white text-sm rounded transition-colors">
                  发送
                </button>
              </div>
            </div>
          </div>
        </div>
      </template>

      <div v-else class="flex-1 flex flex-col items-center justify-center text-gray-400">
        <ChatBubbleLeftRightIcon class="w-16 h-16 mb-4 opacity-50" />
        <p class="text-sm">选择一个会话开始聊天</p>
      </div>
    </div>
  </div>

  <!-- 创建群聊弹窗 -->
  <CreateGroupDialog v-model="showCreateGroup" @created="onGroupCreated" />

  <!-- 群详情抽屉（仅在群会话且有 activeId 时挂载） -->
  <GroupPanel v-if="showGroupPanel && activeConv?.type === 'group' && activeId" v-model:visible="showGroupPanel" :conversation-id="activeId" />

  <!-- 会议中心：建会议 -->
  <CreateMeetingDialog v-model="showMeetingCenter" @created="onMeetingCreated" />
  <!-- 会议房间（Agora 声网） -->
  <MeetingRoom v-model:visible="showMeetingRoom" @ended="onMeetingEnded" />
</template>

<script setup>
/* COMMENTED OUT



import { ref, computed, nextTick, watch, onMounted } from 'vue'
import { useMessagesStore } from '@/stores/messages'
import { useUserStore } from '@/stores/user'
import {
  MagnifyingGlassIcon, ChatBubbleLeftRightIcon, EllipsisHorizontalIcon,
  FaceSmileIcon, UserGroupIcon, UserPlusIcon, ArrowUturnLeftIcon
} from '@heroicons/vue/24/outline'
import CreateGroupDialog from '@/components/CreateGroupDialog.vue'
import GroupPanel from '@/components/GroupPanel.vue'
import AtMembersPopover from '@/components/AtMembersPopover.vue'
import CreateMeetingDialog from '@/components/Meetings/CreateMeetingDialog.vue'
import MeetingRoom from '@/components/Meetings/MeetingRoom.vue'
import { useGroupStore } from '@/stores/group'
import { useMeetingStore } from '@/stores/meeting'
import { useAgora } from '@/composables/useAgora'
import { VideoCameraIcon } from '@heroicons/vue/24/outline'

const messagesStore = useMessagesStore()
const userStore = useUserStore()
const showCreateGroup = ref(false)
const showGroupPanel = ref(false)
const showMeetingCenter = ref(false)
const showMeetingRoom = ref(false)

const meetingStore = useMeetingStore()
const agora = useAgora()

async function onMeetingCreated(meeting) {
  // 创建后自动 join
  showMeetingCenter.value = false
  try {
    const joinPayload = await meetingStore.join(meeting.id)
    await agora.joinMeeting(joinPayload)
    showMeetingRoom.value = true
  } catch (e) {
    ElMessage({ message: '加入会议失败：' + e.message, type: 'error' })
  }
}

async function onMeetingEnded() {
  showMeetingRoom.value = false
  await meetingStore.fetchList()  // 刷新列表
}

// 切换会话时关闭抽屉
watch(() => messagesStore.activeId, () => { showGroupPanel.value = false })

async function onGroupCreated(convId) {
  // 刷新会话列表 + 选中新群
  await messagesStore.fetchConversations()
  if (convId) await messagesStore.selectConversation(convId)
}

/** 群会议入口：用 Agora 声网（替代 P2P mesh） */
async function onGroupVideoCall() {

/** 群会议入口：从群成员 + 通讯录里拉人，开会 */
/** 群会议入口：用 Agora 声网（替代 P2P mesh） */
async function onGroupVideoCall() {
  if (!activeConv.value || activeConv.value.type !== 'group') return
  try {
    // 1. 创建会议
    const meeting = await meetingStore.create({})
    if (!meeting?.id) return
    // 2. 自动加入（获取 Agora token）
    const joinPayload = await meetingStore.join(meeting.id)
    // 3. 加入声网频道
    await agora.joinMeeting(joinPayload)
    // 4. 显示会议房间 UI
    showMeetingRoom.value = true
    ElMessage({ message: '已加入会议，邀请他人加入请分享会议 ID', type: 'success' })
  } catch (e) {
    ElMessage({ message: '加入会议失败：' + e.message, type: 'error' })
  }
}

// ===== @ 成员检测 =====
const groupStore = useGroupStore()
const atShow = ref(false)
const atKeyword = ref('')

const groupMembers = computed(() => {
  if (!activeConv.value || activeConv.value.type !== 'group') return []
  const d = groupStore.details[activeConv.value.id]
  return d?.members || []
})

function onInput(e) {
  const v = inputText.value
  const cursor = v.length  // 简化：假设光标在末尾
  const lastAt = v.lastIndexOf('@', cursor - 1)
  if (lastAt < 0) {
    atShow.value = false
    return
  }
  const after = v.slice(lastAt + 1, cursor)
  if (/\s/.test(after)) {
    atShow.value = false
    return
  }
  atKeyword.value = after
  atShow.value = true
  // 群详情懒加载
  if (activeConv.value?.type === 'group' && !groupStore.details[activeConv.value.id]) {
    groupStore.fetchDetail(activeConv.value.id)
  }
}

function insertAt(m) {
  const v = inputText.value
  const cursor = v.length
  const lastAt = v.lastIndexOf('@', cursor - 1)
  if (lastAt < 0) return
  inputText.value = v.slice(0, lastAt) + `@${m.realName} ` + v.slice(cursor)
  atShow.value = false
  nextTick(() => {
    // 重新聚焦
    document.querySelector('textarea')?.focus()
  })
}

/** 把消息内容里的 @xxx 转成高亮 span（基础版，纯前端，不走后端识别） */
function renderContent(text) {
  if (!text) return ''
  return text.replace(/@([一-龥\w]+)/g,
    '<span class="text-primary font-medium">@$1</span>')
}

/** 加载已读回执（首次 hover 时 lazy load） */
async function loadReaders(msg) {
  if (msg.readersLoaded || msg._loadingReaders) return
  msg._loadingReaders = true
  try {
    const data = messagesStore.fetchMessageReads
      ? await messagesStore.fetchMessageReads(msg.id)
      : null
    if (data && data.readers) {
      msg.readers = data.readers
      msg.readCount = data.readCount ?? data.readers.length
      msg.readersLoaded = true
    }
  } catch (e) {
    // 静默失败 — 复用已有 readCount 即可
  } finally {
    msg._loadingReaders = false
  }
}

const search = ref('')
const inputText = ref('')
const showEmoji = ref(false)
const activeRecall = ref(null)
const messageList = ref(null)

const emojis = ['😀','😁','😂','🤣','😊','😍','😘','😎','🤔','😴','😅','😭','😡','🤯','🥳','👍','👎','👏','🙏','💪','🤝','✌️','👌','👋','🙌','❤️','💔','💕','💯','🔥','⭐','✨','🎉','🎁','🎈','☕','🍕','🚀']

const userInitial = computed(() => userStore.userInfo?.name?.[0] || userStore.userInfo?.username?.[0] || '我')

const conversations = computed(() => messagesStore.conversations)
const activeConv = computed(() => messagesStore.activeConv)
const activeMessages = computed(() => messagesStore.activeMessages)
const loading = computed(() => messagesStore.loading)
const connected = computed(() => messagesStore.connected)

const filteredConvs = computed(() => {
  const kw = search.value.trim().toLowerCase()
  if (!kw) return conversations.value
  return conversations.value.filter(c => (c.name || '').toLowerCase().includes(kw))
})

function peerColor(conv) {
  const palette = ['#3370FF', '#FF7A45', '#00B96B', '#9F7AEA', '#EB2F96', '#F59E0B', '#11CDEF', '#5E72E4']
  if (!conv) return palette[0]
  const name = conv.name || ''
  let h = 0
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) >>> 0
  return palette[h % palette.length]
}

async function selectConv(id) {
  await messagesStore.selectConversation(id)
  scrollBottom()
}

function insertEmoji(e) {
  inputText.value = (inputText.value || '') + e
  showEmoji.value = false
}

async function send() {
  if (!inputText.value.trim()) return
  const text = inputText.value
  inputText.value = ''
  await messagesStore.sendMessage(text)
  scrollBottom()
}

async function doRecall(msgId) {
  await messagesStore.recallMessage(msgId)
  activeRecall.value = null
}

function scrollBottom() {
  nextTick(() => { if (messageList.value) messageList.value.scrollTop = messageList.value.scrollHeight })
}

watch(() => messagesStore.activeId, () => scrollBottom())
watch(() => activeMessages.value?.length, scrollBottom)

onMounted(async () => {
  await messagesStore.fetchConversations()
  // 默认选中第一个
  if (!messagesStore.activeId && conversations.value.length) {
    await messagesStore.selectConversation(conversations.value[0].id)
  }
  // 启动 SignalR（全局生命周期，避免组件 unmount 时打断握手）
  messagesStore.initHub()
})

// 注意：不再 onBeforeUnmount stopHub。
// Hub 生命周期放到 App.vue 全局管理，否则组件卸载会打断握手。

/* vClickOutside DISABLED for testing */

*/
</script>