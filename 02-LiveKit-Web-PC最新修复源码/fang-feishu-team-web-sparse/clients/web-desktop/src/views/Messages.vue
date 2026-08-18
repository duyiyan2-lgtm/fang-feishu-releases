<template>
  <div class="messages-page flex h-full transition-colors">
    <!-- 左：会话列表 -->
    <div class="conversation-panel w-[326px] flex flex-col">
      <div class="conversation-heading h-16 px-5 flex items-center justify-between">
        <h2 class="text-base font-medium text-gray-800 dark:text-gray-100 flex items-center gap-2">
          消息
          <span :class="['w-2 h-2 rounded-full', connected ? 'bg-green-500' : 'bg-gray-300']"
                :title="connected ? 'SignalR 已连接' : '未连接'"></span>
        </h2>
        <div class="flex items-center space-x-1">
          <button @click="showCreateGroup = true" class="new-chat-button" title="发起群聊">
            <UserPlusIcon class="w-4 h-4 text-gray-600 dark:text-gray-300" />
          </button>
        </div>
      </div>

      <div class="conversation-search p-3">
        <div class="relative">
          <MagnifyingGlassIcon class="absolute left-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <input v-model="search" @input="onSearchInput" placeholder="搜索会话/消息"
                 class="w-full h-8 pl-9 pr-3 text-sm bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 focus:bg-white dark:focus:bg-gray-900 focus:ring-2 focus:ring-primary/30 border border-transparent focus:border-primary/40 rounded-md outline-none transition-all dark:text-gray-100" />
          <!-- 全局消息搜索结果下拉 -->
          <div v-if="searchResults.length || searchingMessages"
               class="absolute top-full left-0 right-0 mt-1 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-md shadow-lg max-h-80 overflow-y-auto z-10">
            <div v-if="searchingMessages" class="p-3 text-xs text-gray-400 text-center">搜索中…</div>
            <div v-else-if="searchResults.length === 0" class="p-3 text-xs text-gray-400 text-center">无匹配消息</div>
            <div v-else>
              <div class="px-3 py-1.5 text-xs text-gray-500 border-b border-gray-100 dark:border-gray-800">消息搜索 · {{ searchResults.length }} 条</div>
              <div v-for="(r, idx) in searchResults" :key="idx"
                   @click="openSearchResult(r)"
                   class="px-3 py-2 hover:bg-gray-50 dark:hover:bg-gray-800 cursor-pointer border-b border-gray-50 dark:border-gray-800/50 last:border-0">
                <div class="flex items-center gap-1 text-xs text-gray-500">
                  <span class="font-medium text-primary">{{ r.conversationTitle || r.conversationType || '会话' }}</span>
                  <span>·</span>
                  <span>{{ r.message?.senderName || '' }}</span>
                  <span>·</span>
                  <span class="text-gray-400">{{ r.message?.createdAt?.slice(0, 10) }}</span>
                </div>
                <div class="text-sm text-gray-700 dark:text-gray-200 mt-1 line-clamp-2">{{ r.message?.content || '(空)' }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="conversation-list flex-1 overflow-y-auto px-2 pb-3">
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
             class="conversation-item group relative px-3 py-3 cursor-pointer transition-colors"
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
            <button @click.stop="onDeleteConv(conv)" title="删除会话"
                    class="absolute right-2 top-1/2 -translate-y-1/2 w-5 h-5 rounded hover:bg-red-50 dark:hover:bg-red-900/30 text-gray-400 hover:text-red-500 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
              <XMarkIcon class="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- 右：聊天窗口 -->
    <div class="chat-panel flex-1 flex flex-col">
      <template v-if="activeConv">
        <div class="chat-heading h-16 px-6 flex items-center justify-between">
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
                    class="meeting-primary h-9 px-3 rounded-xl flex items-center text-white"
                    title="群会议（自建 LiveKit 多人视频）">
              <VideoCameraIcon class="w-4 h-4" />
              <span class="ml-1 text-xs font-medium">群会议</span>
            </button>
            <button @click="showMeetingCenter = true"
                    class="meeting-secondary w-9 h-9 rounded-xl flex items-center justify-center"
                    title="会议中心（自建 LiveKit）">
              <VideoCameraIcon class="w-4 h-4 text-gray-600 dark:text-gray-300" />
            </button>
            <div class="relative">
              <button @click="showMoreMenu = !showMoreMenu"
                      class="w-8 h-8 rounded-md hover:bg-gray-100 dark:hover:bg-gray-800 flex items-center justify-center"
                      title="更多">
                <EllipsisHorizontalIcon class="w-4 h-4 text-gray-600 dark:text-gray-300" />
              </button>
              <transition name="menu">
                <div v-if="showMoreMenu" data-more-menu-root @click.stop
                     class="absolute right-0 top-full mt-1 w-44 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-md shadow-lg z-20 py-1">
                  <button @click="markAllRead; showMoreMenu = false"
                          class="w-full text-left px-3 py-2 text-sm hover:bg-gray-50 dark:hover:bg-gray-800 flex items-center">
                    <CheckCircleIcon class="w-4 h-4 mr-2 text-gray-500" />
                    全部标为已读
                  </button>
                  <button @click="onMuteClick"
                          :class="['w-full text-left px-3 py-2 text-sm hover:bg-gray-50 dark:hover:bg-gray-800 flex items-center',
                                   muted ? 'text-primary' : '']">
                    <BellSlashIcon v-if="muted" class="w-4 h-4 mr-2 text-gray-500" />
                    <BellIcon v-else class="w-4 h-4 mr-2 text-gray-500" />
                    {{ muted ? '取消静音' : '消息免打扰' }}
                  </button>
                </div>
              </transition>
            </div>
          </div>
        </div>

        <div ref="messageList" class="message-stage flex-1 overflow-y-auto px-6 py-6 relative">
          <div class="message-stream max-w-3xl mx-auto space-y-3">
            <div v-for="msg in activeMessages" :key="msg.id"
                 :class="['message-row flex', msg.sender === 'me' ? 'justify-end' : 'justify-start']">
              <div v-if="msg.sender !== 'me'" class="w-8 h-8 rounded-md flex-shrink-0 flex items-center justify-center text-white text-xs font-medium mr-2"
                   :style="{ background: peerColor(activeConv) }">{{ activeConv.name?.[0] }}</div>

              <div class="message-bubble-wrap group relative max-w-md" :class="msg.sender === 'me' ? 'ml-2 mr-1' : 'ml-2'">
                <div v-if="msg.sender !== 'me' && msg.senderName" class="text-xs text-gray-500 ml-1 mb-1">{{ msg.senderName }}</div>
                <div :class="['message-bubble px-4 py-2 rounded-xl text-sm',
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
                    <span v-else-if="msg.failed" class="message-failed">发送失败</span>
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

        <div class="composer-shell px-6 py-3">
          <div class="max-w-3xl mx-auto">
            <div class="composer-card overflow-hidden transition-colors">
              <textarea ref="composerInput" v-model="inputText" @input="onInput" @keydown.enter.exact.prevent="send" rows="3"
                        placeholder="输入消息，回车发送，Shift+回车换行；输入 @ 选择成员"
                        class="w-full px-3 py-2 text-sm bg-white dark:bg-gray-900 outline-none resize-none text-gray-800 dark:text-gray-100 placeholder-gray-400" />

              <AtMembersPopover :show="atShow"
                                 :keyword="atKeyword"
                                 :members="groupMembers"
                                 @pick="insertAt" />
              <div class="flex items-center justify-between px-2 py-1.5 border-t border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50 relative">
                <div class="flex items-center space-x-1">
                  <div class="relative">
                    <button @click="showEmoji = !showEmoji" @blur="closeEmojiLater"
                            class="w-7 h-7 rounded hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center justify-center">
                      <FaceSmileIcon class="w-4 h-4 text-gray-500" />
                    </button>
                    <div v-if="showEmoji" @click.stop
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

      <div v-else class="chat-empty flex-1 flex flex-col items-center justify-center text-gray-400">
        <div class="empty-illustration"><ChatBubbleLeftRightIcon /></div>
        <h3>让沟通从这里开始</h3>
        <p>选择左侧会话，或发起一个新的团队群聊</p>
        <button @click="showCreateGroup = true"><UserPlusIcon />发起群聊</button>
      </div>
    </div>
  </div>

  <!-- 创建群聊弹窗 -->
  <CreateGroupDialog v-model="showCreateGroup" @created="onGroupCreated" />

  <!-- 群详情抽屉（仅在群会话且有 activeId 时挂载） -->
  <GroupPanel v-if="showGroupPanel && activeConv?.type === 'group' && messagesStore.activeId" v-model:visible="showGroupPanel" :conversation-id="messagesStore.activeId" />

  <!-- 会议中心：列出现有会议 + 创建 -->
  <MeetingCenterDialog v-model="showMeetingCenter" @join="onMeetingJoinFromList" @createNew="onCreateMeetingFromCenter" />
  <!-- 兼容老入口（直接弹出创建）-->
  <CreateMeetingDialog v-model="showCreateMeeting" @created="onMeetingCreated" />
  <!-- 会议房间（自建 LiveKit） -->
  <MeetingRoom v-if="showMeetingRoom" v-model:visible="showMeetingRoom" @ended="onMeetingEnded" />
</template>

<script setup>
// 局部 v-click-outside 指令
const vClickOutside = {
  mounted(el, binding) {
    el._clickOutsideHandler = (e) => {
      if (!el.contains(e.target)) binding.value(e)
    }
    setTimeout(() => document.addEventListener('click', el._clickOutsideHandler), 0)
  },
  unmounted(el) {
    document.removeEventListener('click', el._clickOutsideHandler)
  }
}


import { ref, computed, defineAsyncComponent, nextTick, watch, onMounted, onUnmounted, onBeforeUnmount } from 'vue'
import { useMessagesStore } from '@/stores/messages'
import { useUserStore } from '@/stores/user'
import {
  MagnifyingGlassIcon, ChatBubbleLeftRightIcon, EllipsisHorizontalIcon,
  FaceSmileIcon, UserGroupIcon, UserPlusIcon, ArrowUturnLeftIcon, XMarkIcon,
  CheckCircleIcon, BellSlashIcon, BellIcon
} from '@heroicons/vue/24/outline'
import CreateGroupDialog from '@/components/CreateGroupDialog.vue'
import GroupPanel from '@/components/GroupPanel.vue'
import AtMembersPopover from '@/components/AtMembersPopover.vue'
import CreateMeetingDialog from '@/components/Meetings/CreateMeetingDialog.vue'
import MeetingCenterDialog from '@/components/Meetings/MeetingCenterDialog.vue'
import { useGroupStore } from '@/stores/group'
import { useMeetingStore } from '@/stores/meeting'
import { ElMessage } from '@/api/toast'
import { VideoCameraIcon } from '@heroicons/vue/24/outline'

// 视频会议属于低频重功能，只有真正进入会议时才下载 LiveKit 与房间界面。
const MeetingRoom = defineAsyncComponent(() => import('@/components/Meetings/MeetingRoom.vue'))

const messagesStore = useMessagesStore()
const userStore = useUserStore()
const showCreateGroup = ref(false)
const showGroupPanel = ref(false)
const showMoreMenu = ref(false)
const muted = ref(false)
const showMeetingCenter = ref(false)
const showCreateMeeting = ref(false)
const showMeetingRoom = ref(false)

const meetingStore = useMeetingStore()
let liveKitInstance = null
async function getLiveKit() {
  if (!liveKitInstance) {
    const { useLiveKit } = await import('@/composables/useLiveKit')
    liveKitInstance = useLiveKit()
  }
  return liveKitInstance
}

async function onMeetingCreated(meeting) {
  // 创建后自动 join
  showCreateMeeting.value = false
  showMeetingCenter.value = false
  try {
    const joinPayload = await meetingStore.join(meeting.id)
    const liveKit = await getLiveKit()
    await liveKit.joinMeeting(joinPayload)
    showMeetingRoom.value = true
  } catch (e) {
    ElMessage({ message: '加入会议失败：' + e.message, type: 'error' })
  }
}

/** 会议中心：点 "加入" 按钮 */
async function onMeetingJoinFromList(meeting, joinPayload) {
  try {
    // joinPayload 已经在 store.join 里拿过；再 join 一次确保有 payload
    const data = joinPayload || await meetingStore.join(meeting.id)
    const liveKit = await getLiveKit()
    await liveKit.joinMeeting(data)
    showMeetingRoom.value = true
  } catch (e) {
    ElMessage({ message: '加入会议失败：' + (e?.message || ''), type: 'error' })
  }
}

/** 会议中心：点 "创建新会议" 按钮 */
function onCreateMeetingFromCenter() {
  showCreateMeeting.value = true
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

/** 群会议入口：使用自建 LiveKit SFU（替代 Agora / P2P mesh） */
async function onGroupVideoCall() {
  if (!activeConv.value || activeConv.value.type !== 'group') return
  try {
    // 先确保成员详情已加载，否则首次进入群聊就开会时会错误地邀请 0 人。
    const conversationId = activeConv.value.id
    const detail = groupStore.details[conversationId] || await groupStore.fetchDetail(conversationId)
    if (!detail) throw new Error('无法读取群成员，请稍后重试')
    const memberIds = (detail.members || [])
      .map(m => m.userId)
      .filter(id => id && id !== userStore.userInfo?.id)
    // 1. 创建会议（同一个 channelName 会生成，固定）
    const meeting = await meetingStore.create({ title: `${activeConv.value.name || '群聊'}的视频会议` })
    if (!meeting?.id) return
    // 2. 邀请所有群成员加入同一个 LiveKit 房间
    if (memberIds.length > 0) {
      await meetingStore.inviteMembers(meeting.id, memberIds)
    }
    // 3. 创建者自动加入（获取 LiveKit 临时令牌）
    const joinPayload = await meetingStore.join(meeting.id)
    // 4. 加入自建 LiveKit 房间
    const liveKit = await getLiveKit()
    await liveKit.joinMeeting(joinPayload)
    // 5. 显示会议房间 UI
    showMeetingRoom.value = true
    ElMessage({ message: `群会议已开启，已邀请 ${memberIds.length} 人（他们会收到通知）`, type: 'success' })
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
  const cursor = e?.target?.selectionStart ?? v.length
  composerCursor.value = cursor
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
  const cursor = composerCursor.value || v.length
  const lastAt = v.lastIndexOf('@', cursor - 1)
  if (lastAt < 0) return
  const mention = `@${m.realName} `
  inputText.value = v.slice(0, lastAt) + mention + v.slice(cursor)
  const nextCursor = lastAt + mention.length
  composerCursor.value = nextCursor
  atShow.value = false
  nextTick(() => {
    composerInput.value?.focus()
    composerInput.value?.setSelectionRange(nextCursor, nextCursor)
  })
}

/** 转义消息原文后，再把 @xxx 转成高亮 span */
function renderContent(text) {
  if (!text) return ''
  const escaped = String(text).replace(/[&<>"']/g, (char) => ({
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;'
  })[char])
  return escaped.replace(/@([一-龥\w]+)/g,
    '<span class="text-primary font-medium">@$1</span>')
}

/** 加载已读回执（首次 hover 时 lazy load） */
async function loadReaders(msg) {
  if (msg.readersLoaded || msg._loadingReaders) return
  msg._loadingReaders = true
  try {
    const data = await messagesStore.fetchMessageReads(msg.id)
    msg.readers = data.readers || []
    msg.readCount = data.readCount ?? msg.readers.length
    msg.readersLoaded = true
  } catch (e) {
    // 静默失败 — 复用已有 readCount 即可
  } finally {
    msg._loadingReaders = false
  }
}

const search = ref('')
const inputText = ref('')
const composerInput = ref(null)
const composerCursor = ref(0)
const showEmoji = ref(false)
const activeRecall = ref(null)
const messageList = ref(null)
let emojiCloseTimer = null
function closeEmojiLater() {
  if (emojiCloseTimer) clearTimeout(emojiCloseTimer)
  emojiCloseTimer = setTimeout(() => { showEmoji.value = false }, 200)
}

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

// 全局消息搜索（后端 GET /im/messages/search）
const searchResults = ref([])
const searchingMessages = ref(false)
let searchMsgTimer = null
function onSearchInput() {
  if (searchMsgTimer) clearTimeout(searchMsgTimer)
  const kw = search.value.trim()
  if (!kw) { searchResults.value = []; return }
  searchMsgTimer = setTimeout(async () => {
    searchingMessages.value = true
    try {
      const { searchMessages } = await import('@/api/im')
      searchResults.value = await searchMessages(kw)
    } catch (e) {
      searchResults.value = []
    } finally {
      searchingMessages.value = false
    }
  }, 400) // debounce 400ms
}

function openSearchResult(r) {
  // 切到该会话
  if (r.conversationId) {
    selectConv(r.conversationId)
  }
  search.value = ''
  searchResults.value = []
}

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

async function markAllRead() {
  showMoreMenu.value = false
  const convs = messagesStore.conversations || []
  if (convs.length === 0) return

  const results = await Promise.allSettled(
    convs
      .filter(conv => conv.id)
      .map(async conv => {
        const { markConversationRead } = await import('@/api/im')
        await markConversationRead(conv.id)
        conv.unread = 0
      })
  )
  const failed = results.filter(result => result.status === 'rejected').length
  if (failed === 0) {
    ElMessage({ message: '已标记全部已读', type: 'success' })
  } else {
    ElMessage({ message: `${failed} 个会话标记失败`, type: 'error' })
  }
}

function toggleMute() {
  muted.value = !muted.value
  ElMessage({ message: muted.value ? '已开启消息免打扰' : '已关闭消息免打扰', type: 'info' })
}
function onMuteClick() {
  toggleMute()
  showMoreMenu.value = false
}

// 三点菜单的 click-outside 关闭
function onDocClickMore(e) {
  if (!showMoreMenu.value) return
  const root = document.querySelector('[data-more-menu-root]')
  // @click.stop 已经在菜单 div 上阻止了内部点击；这里只关外部
  if (root && !root.contains(e.target)) {
    // 也要排除"更多"按钮本身（按钮在 root 之外）
    const btn = document.querySelector('button[title="更多"]')
    if (btn && btn.contains(e.target)) return
    showMoreMenu.value = false
  }
}
onMounted(() => document.addEventListener('click', onDocClickMore))
onBeforeUnmount(() => document.removeEventListener('click', onDocClickMore))

async function onDeleteConv(conv) {
  if (!confirm(`确定删除会话「${conv.name || '未命名'}」？此操作不可恢复。`)) return
  try {
    const { deleteConversation } = await import('@/api/im')
    await deleteConversation(conv.id)
    // 从 store 移除
    messagesStore.conversations = messagesStore.conversations.filter(c => c.id !== conv.id)
    if (messagesStore.activeId === conv.id) {
      messagesStore.activeId = null
    }
    ElMessage({ message: '已删除会话', type: 'success' })
  } catch (e) {
    ElMessage({ message: '删除失败：' + (e?.message || ''), type: 'error' })
  }
}

function onConvContextMenu(conv, e) {
  // 右键只阻止浏览器默认菜单，不执行删除操作。
  // 删除会话必须通过行内删除按钮，并经过确认。
}

function insertEmoji(e) {
  inputText.value = (inputText.value || '') + e
  showEmoji.value = false
}

async function send() {
  if (!inputText.value.trim()) return
  const text = inputText.value
  inputText.value = ''
  const sent = await messagesStore.sendMessage(text)
  if (!sent && !inputText.value) inputText.value = text
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

// 页面回到前台时停止标题闪烁 — 在 setup 顶层（onUnmounted 必须在同步部分）
const onVisibilityChange = () => { /* placeholder, real handler set after onMounted */ }
document.addEventListener('visibilitychange', onVisibilityChange)
onUnmounted(() => {
  document.removeEventListener('visibilitychange', onVisibilityChange)
})

onMounted(async () => {
  await messagesStore.fetchConversations()
  // 默认选中第一个
  if (!messagesStore.activeId && conversations.value.length) {
    await messagesStore.selectConversation(conversations.value[0].id)
  }
  // 启动 SignalR（全局生命周期，避免组件 unmount 时打断握手）
  messagesStore.initHub()

  // 申请系统通知权限（避免顶层 await）
  const notif = await import('@/utils/notification')
  notif.captureOriginalTitle()
  if ('Notification' in window && Notification.permission === 'default') {
    setTimeout(() => {
      notif.requestNotificationPermission().then(r => {
        if (r.ok) console.info('[notification] 通知权限已开启')
      })
    }, 2000)
  }
})

// 注意：不再 onBeforeUnmount stopHub。
// Hub 生命周期放到 App.vue 全局管理，否则组件卸载会打断握手。

// 监听 SignalR 推送的会议结束事件
async function onSignalRMeetingEnded() {
  showMeetingRoom.value = false
  // 同时清理后端参会状态和浏览器 LiveKit 连接，避免摄像头仍被占用。
  if (meetingStore.current) {
    await meetingStore.leave(meetingStore.current.id)
  }
  const liveKit = await getLiveKit()
  await liveKit.leaveMeeting()
}
window.addEventListener('meeting-ended', onSignalRMeetingEnded)
onUnmounted(() => {
  window.removeEventListener('meeting-ended', onSignalRMeetingEnded)
  if (liveKitInstance?.state?.value !== 'idle') {
    const activeMeetingId = meetingStore.current?.id
    if (activeMeetingId) void meetingStore.leave(activeMeetingId)
    void liveKitInstance.leaveMeeting()
  }
})

/* vClickOutside DISABLED for testing */
</script>

<style scoped>
.messages-page { color: var(--text-primary); background: var(--surface-soft); }
.conversation-panel { border-right: 1px solid var(--border-subtle); background: var(--surface); }
.conversation-heading,.chat-heading { border-bottom: 1px solid var(--border-subtle); background: var(--surface-elevated); }
.conversation-heading h2 { font-weight: 750; letter-spacing: -.01em; }
.new-chat-button { display: grid; width: 34px; height: 34px; place-items: center; border: 1px solid var(--border-subtle); border-radius: 11px; background: var(--surface-soft); transition: .18s ease; }
.new-chat-button:hover { border-color: rgba(53,104,244,.26); background: var(--brand-soft); transform: translateY(-1px); }.new-chat-button :deep(svg) { color: var(--brand); }
.conversation-search { border-bottom: 1px solid var(--border-subtle); background: var(--surface); }
.conversation-search input { height: 38px; border-radius: 12px; color: var(--text-primary); background: var(--surface-soft); }
.conversation-list { background: linear-gradient(180deg,var(--surface),var(--surface-soft)); }
.conversation-item { margin: 3px 0; border: 1px solid transparent !important; border-radius: 14px; }
.conversation-item:hover { border-color: var(--border-subtle) !important; background: var(--surface) !important; box-shadow: 0 8px 24px rgba(34,54,96,.06); }
.conversation-item.bg-primary-50 { border-color: rgba(53,104,244,.14) !important; background: var(--brand-soft) !important; box-shadow: inset 3px 0 0 var(--brand); }
.chat-panel { min-width: 0; background: var(--surface-soft); }
.chat-heading { box-shadow: 0 8px 24px rgba(36,58,100,.035); }
.meeting-primary { background: linear-gradient(135deg,#4379ff,#6b5df3); box-shadow: 0 7px 18px rgba(67,121,255,.22); transition: .18s ease; }.meeting-primary:hover { transform: translateY(-1px); box-shadow: 0 10px 24px rgba(67,121,255,.3); }
.meeting-secondary { border: 1px solid var(--border-subtle); background: var(--surface-soft); transition: .18s ease; }.meeting-secondary:hover { border-color: rgba(53,104,244,.25); background: var(--brand-soft); }
.message-stage { background: radial-gradient(circle at 82% 7%,rgba(72,121,255,.07),transparent 25%), linear-gradient(180deg,var(--surface-soft),var(--app-bg)); }
.message-stream { padding: 4px 0 22px; }
.message-row { content-visibility: auto; contain-intrinsic-size: 64px; }
.message-failed { color: #fecaca; font-weight: 650; }
.message-bubble { border: 1px solid var(--border-subtle); box-shadow: 0 7px 18px rgba(39,58,95,.055); line-height: 1.55; }
.message-bubble.bg-primary { border-color: transparent; background: linear-gradient(135deg,#3f73f4,#3564df) !important; box-shadow: 0 8px 20px rgba(53,100,223,.18); }
.message-bubble-wrap { animation: bubble-in .18s ease both; }
.composer-shell { border-top: 1px solid var(--border-subtle); background: var(--surface-elevated); backdrop-filter: blur(16px); }
.composer-card { border: 1px solid var(--border-subtle); border-radius: 15px; background: var(--surface); box-shadow: 0 9px 28px rgba(38,59,102,.055); }
.composer-card:focus-within { border-color: rgba(53,104,244,.48); box-shadow: 0 0 0 3px rgba(53,104,244,.08),0 10px 30px rgba(38,59,102,.08); }
.composer-card textarea { background: transparent; }
.chat-empty { color: var(--text-tertiary); background: radial-gradient(circle at 50% 40%,rgba(80,126,244,.1),transparent 32%); }.empty-illustration { display: grid; width: 82px; height: 82px; margin-bottom: 18px; place-items: center; border: 1px solid rgba(53,104,244,.16); border-radius: 26px; color: var(--brand); background: var(--brand-soft); box-shadow: 0 18px 38px rgba(53,104,244,.12); }.empty-illustration svg { width: 34px; }.chat-empty h3 { color: var(--text-primary); font-size: 16px; font-weight: 750; }.chat-empty p { margin-top: 6px; font-size: 11px; }.chat-empty button { display: flex; align-items: center; gap: 7px; height: 36px; margin-top: 18px; padding: 0 14px; border-radius: 11px; color: white; background: var(--brand); font-size: 11px; font-weight: 650; box-shadow: 0 8px 20px rgba(53,104,244,.2); }.chat-empty button svg { width: 15px; }
@keyframes bubble-in { from { opacity: 0; transform: translateY(5px); } }
</style>
