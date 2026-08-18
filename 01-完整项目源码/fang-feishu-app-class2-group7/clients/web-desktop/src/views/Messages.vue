<template>
  <div class="flex h-full bg-white dark:bg-gray-900 transition-colors duration-200">
    <!-- 左：会话列表 -->
    <div class="w-[300px] xl:w-80 border-r border-line dark:border-gray-700/80 flex flex-col bg-white dark:bg-gray-900 flex-shrink-0">
      <div class="h-13 px-4 flex items-center justify-between border-b border-line-soft dark:border-gray-700/80">
        <h2 class="text-[15px] font-semibold text-ink dark:text-gray-100 flex items-center gap-2">
          消息
          <span
            class="w-2 h-2 rounded-full transition-colors"
            :class="connected ? 'bg-emerald-500 shadow-[0_0_0_3px_rgba(16,185,129,0.2)]' : 'bg-gray-300 dark:bg-gray-600'"
            :title="connected ? '实时连接已就绪' : '连接中…'"
          />
        </h2>
        <div class="flex items-center gap-0.5">
          <button type="button" class="ff-icon-btn" title="发起群聊" @click="showCreateGroup = true">
            <UserPlusIcon class="w-4 h-4" />
          </button>
        </div>
      </div>

      <div class="p-3 border-b border-line-soft dark:border-gray-700/80">
        <div class="relative">
          <MagnifyingGlassIcon class="absolute left-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-ink-tertiary pointer-events-none" />
          <input
            v-model="search"
            placeholder="搜索会话 / 消息"
            class="ff-input pl-9"
            @input="onSearchInput"
          />
          <!-- 全局消息搜索结果下拉 -->
          <div
            v-if="searchResults.length || searchingMessages"
            class="absolute top-full left-0 right-0 mt-1.5 bg-white dark:bg-gray-900 border border-line dark:border-gray-700 rounded-lg shadow-float max-h-80 overflow-y-auto z-10 scrollbar-auto"
          >
            <div v-if="searchingMessages" class="p-3 text-xs text-ink-tertiary text-center">搜索中…</div>
            <div v-else-if="searchResults.length === 0" class="p-3 text-xs text-ink-tertiary text-center">无匹配消息</div>
            <div v-else>
              <div class="px-3 py-1.5 text-2xs text-ink-tertiary border-b border-line-soft dark:border-gray-800 sticky top-0 bg-white/95 dark:bg-gray-900/95 backdrop-blur">
                消息搜索 · {{ searchResults.length }} 条
              </div>
              <div
                v-for="(r, idx) in searchResults"
                :key="idx"
                class="px-3 py-2.5 hover:bg-primary-soft dark:hover:bg-primary/10 cursor-pointer border-b border-line-soft/60 dark:border-gray-800/50 last:border-0 transition-colors"
                @click="openSearchResult(r)"
              >
                <div class="flex items-center gap-1 text-2xs text-ink-tertiary">
                  <span class="font-medium text-primary">{{ r.conversationTitle || r.conversationType || '会话' }}</span>
                  <span>·</span>
                  <span>{{ r.message?.senderName || '' }}</span>
                  <span>·</span>
                  <span>{{ r.message?.createdAt?.slice(0, 10) }}</span>
                </div>
                <div class="text-sm text-ink dark:text-gray-200 mt-1 line-clamp-2">{{ r.message?.content || '(空)' }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div
        ref="convListRef"
        class="flex-1 overflow-y-auto scrollbar-auto"
        @scroll="onConvScroll"
      >
        <div v-if="loading && conversations.length === 0" class="p-8 text-center text-sm text-ink-tertiary">
          <svg class="animate-spin w-5 h-5 mx-auto mb-2 text-primary" viewBox="0 0 24 24" fill="none">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
            <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
          </svg>
          加载中…
        </div>
        <div v-else-if="filteredConvs.length === 0" class="p-10 text-center">
          <ChatBubbleLeftRightIcon class="w-10 h-10 mx-auto mb-3 text-ink-tertiary/40" />
          <p class="text-sm text-ink-tertiary">没有找到会话</p>
        </div>

        <!-- 会话虚拟列表 -->
        <div v-else :style="{ height: convTotalHeight + 'px', position: 'relative' }">
          <div :style="{ transform: `translateY(${convOffsetY}px)` }">
            <div
              v-for="row in convVisible"
              :key="row.key"
              class="group relative px-3 py-2.5 mx-1.5 rounded-lg cursor-pointer transition-colors duration-120"
              :style="{ height: convItemHeight + 'px', boxSizing: 'border-box' }"
              :class="messagesStore.activeId === row.data.id
                ? 'bg-primary-soft dark:bg-primary/15'
                : 'hover:bg-surface-secondary dark:hover:bg-gray-800/80'"
              @click="selectConv(row.data.id)"
            >
              <div class="flex items-center h-full">
                <div class="relative flex-shrink-0">
                  <div
                    class="w-10 h-10 rounded-lg flex items-center justify-center text-white font-semibold text-sm shadow-sm"
                    :style="{ background: peerColor(row.data) }"
                  >{{ row.data.name?.[0] || '?' }}</div>
                  <span
                    v-if="row.data.unread"
                    class="absolute -top-1 -right-1 ff-badge"
                  >{{ row.data.unread > 99 ? '99+' : row.data.unread }}</span>
                </div>
                <div class="ml-3 flex-1 min-w-0">
                  <div class="flex justify-between items-baseline gap-2">
                    <span class="font-medium text-sm text-ink dark:text-gray-100 truncate flex items-center gap-1">
                      {{ row.data.name }}
                      <UserGroupIcon v-if="row.data.type === 'group'" class="w-3.5 h-3.5 text-ink-tertiary flex-shrink-0" />
                    </span>
                    <span class="text-2xs text-ink-tertiary flex-shrink-0">{{ row.data.lastTime }}</span>
                  </div>
                  <p class="mt-0.5 text-[13px] text-ink-secondary dark:text-gray-400 truncate">
                    <span v-if="row.data.lastIsRecalled" class="italic text-ink-tertiary">消息已撤回</span>
                    <span v-else>
                      <span v-if="row.data.lastSender" class="text-ink-tertiary">{{ row.data.lastSender }}: </span>{{ row.data.lastMessage }}
                    </span>
                  </p>
                </div>
                <button
                  type="button"
                  title="删除会话"
                  class="absolute right-2 top-1/2 -translate-y-1/2 w-6 h-6 rounded-md
                         hover:bg-red-50 dark:hover:bg-red-900/30 text-ink-tertiary hover:text-red-500
                         opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center"
                  @click.stop="onDeleteConv(row.data)"
                >
                  <XMarkIcon class="w-3.5 h-3.5" />
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 右：聊天窗口 -->
    <div class="flex-1 flex flex-col min-w-0 bg-[#F7F8FA] dark:bg-[#12151C]">
      <template v-if="activeConv">
        <div class="h-13 px-5 flex items-center justify-between border-b border-line-soft dark:border-gray-700/80 bg-white/90 dark:bg-gray-900/90 backdrop-blur-sm flex-shrink-0">
          <div class="flex items-center min-w-0">
            <div
              class="w-9 h-9 rounded-lg flex items-center justify-center text-white font-semibold text-sm flex-shrink-0 shadow-sm"
              :style="{ background: peerColor(activeConv) }"
            >{{ activeConv.name?.[0] || '?' }}</div>
            <div class="ml-3 min-w-0">
              <div class="font-semibold text-sm text-ink dark:text-gray-100 truncate">{{ activeConv.name }}</div>
              <div class="text-2xs text-ink-tertiary flex items-center mt-0.5">
                <span
                  class="inline-block w-1.5 h-1.5 rounded-full mr-1.5"
                  :class="connected ? 'bg-emerald-500' : 'bg-gray-400'"
                />
                {{ connected ? '实时在线' : '离线' }}
              </div>
            </div>
          </div>
          <div class="flex items-center gap-1 flex-shrink-0">
            <button
              v-if="activeConv?.type === 'group'"
              type="button"
              class="ff-icon-btn"
              title="群详情"
              @click="showGroupPanel = true"
            >
              <UserGroupIcon class="w-4 h-4" />
            </button>
            <button
              v-if="activeConv?.type === 'group'"
              type="button"
              class="h-8 px-3 rounded-md flex items-center gap-1.5 bg-gradient-to-r from-primary to-violet-500 text-white text-xs font-medium shadow-sm hover:opacity-95 active:scale-[0.98] transition-all"
              title="群会议（Agora 多人视频）"
              @click="onGroupVideoCall"
            >
              <VideoCameraIcon class="w-4 h-4" />
              群会议
            </button>
            <button type="button" class="ff-icon-btn" title="会议中心" @click="showMeetingCenter = true">
              <VideoCameraIcon class="w-4 h-4" />
            </button>
            <div class="relative">
              <button type="button" class="ff-icon-btn" title="更多" @click="showMoreMenu = !showMoreMenu">
                <EllipsisHorizontalIcon class="w-4 h-4" />
              </button>
              <transition name="menu">
                <div
                  v-if="showMoreMenu"
                  data-more-menu-root
                  class="absolute right-0 top-full mt-1.5 w-48 bg-white dark:bg-gray-900 border border-line dark:border-gray-700 rounded-lg shadow-float z-20 py-1.5"
                  @click.stop
                >
                  <button
                    type="button"
                    class="w-full text-left px-3 py-2 text-sm hover:bg-surface-secondary dark:hover:bg-gray-800 flex items-center gap-2 text-ink dark:text-gray-200"
                    @click="markAllRead; showMoreMenu = false"
                  >
                    <CheckCircleIcon class="w-4 h-4 text-ink-tertiary" />
                    全部标为已读
                  </button>
                  <button
                    type="button"
                    class="w-full text-left px-3 py-2 text-sm hover:bg-surface-secondary dark:hover:bg-gray-800 flex items-center gap-2"
                    :class="muted ? 'text-primary' : 'text-ink dark:text-gray-200'"
                    @click="onMuteClick"
                  >
                    <BellSlashIcon v-if="muted" class="w-4 h-4 text-ink-tertiary" />
                    <BellIcon v-else class="w-4 h-4 text-ink-tertiary" />
                    {{ muted ? '取消静音' : '消息免打扰' }}
                  </button>
                </div>
              </transition>
            </div>
          </div>
        </div>

        <div
          ref="msgListRef"
          class="flex-1 overflow-y-auto scroll-y px-5 py-4 relative scrollbar-auto"
          @scroll="onMsgScroll"
        >
          <!-- 消息虚拟列表（固定估算行高 + overscan，长会话更流畅） -->
          <div class="max-w-3xl mx-auto" :style="{ height: msgTotalHeight + 'px', position: 'relative' }">
            <div :style="{ transform: `translateY(${msgOffsetY}px)` }">
              <div
                v-for="row in msgVisible"
                :key="row.key"
                class="flex px-0"
                :class="row.data.sender === 'me' ? 'justify-end' : 'justify-start'"
                :style="{ height: msgItemHeight + 'px', boxSizing: 'border-box', paddingTop: '6px', paddingBottom: '6px' }"
              >
                <div
                  v-if="row.data.sender !== 'me'"
                  class="w-8 h-8 rounded-lg flex-shrink-0 flex items-center justify-center text-white text-xs font-semibold mr-2 shadow-sm self-start"
                  :style="{ background: peerColor(activeConv) }"
                >{{ activeConv.name?.[0] }}</div>

                <div class="group relative max-w-[min(28rem,75%)] self-start" :class="row.data.sender === 'me' ? 'ml-2 mr-1' : 'ml-1'">
                  <div v-if="row.data.sender !== 'me' && row.data.senderName" class="text-2xs text-ink-tertiary ml-1 mb-0.5 truncate">{{ row.data.senderName }}</div>
                  <div
                    class="px-3.5 py-2 text-sm shadow-soft leading-relaxed max-h-[calc(100%-4px)] overflow-hidden"
                    :class="[
                      row.data.recalled
                        ? 'rounded-lg bg-surface-tertiary dark:bg-gray-800 text-ink-tertiary italic'
                        : row.data.sender === 'me'
                          ? 'rounded-2xl rounded-br-md bg-primary text-white'
                          : 'rounded-2xl rounded-bl-md bg-white dark:bg-gray-800 text-ink dark:text-gray-100 border border-line-soft/80 dark:border-gray-700/50'
                    ]"
                  >
                    <div v-if="row.data.recalled" class="flex items-center text-xs">
                      <ArrowUturnLeftIcon class="w-3 h-3 mr-1" />
                      {{ row.data.sender === 'me' ? '你' : row.data.senderName }} 撤回了一条消息
                    </div>
                    <div v-else class="whitespace-pre-wrap break-words line-clamp-4" v-html="renderContent(row.data.content)" />
                    <div
                      class="text-[10px] mt-1 text-right flex items-center justify-end gap-1.5"
                      :class="row.data.recalled ? 'text-ink-tertiary' : row.data.sender === 'me' ? 'text-white/65' : 'text-ink-tertiary'"
                    >
                      <span v-if="row.data.pending" class="italic opacity-80">发送中…</span>
                      <span>{{ row.data.time }}</span>
                      <span
                        v-if="row.data.sender === 'me' && !row.data.pending && !row.data.recalled && (row.data.readCount || 0) >= 1"
                        class="cursor-pointer hover:opacity-80"
                        @mouseenter="loadReaders(row.data)"
                      >
                        已读 {{ row.data.readCount }}
                      </span>
                    </div>
                  </div>

                  <button
                    v-if="row.data.sender === 'me' && !row.data.recalled"
                    type="button"
                    class="absolute -top-2 right-1 w-5 h-5 bg-white dark:bg-gray-700 rounded-full shadow-soft opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center text-ink-tertiary hover:text-primary"
                    @click.stop="activeRecall = row.data.id"
                  >
                    <EllipsisHorizontalIcon class="w-3 h-3" />
                  </button>
                </div>

                <div
                  v-if="row.data.sender === 'me'"
                  class="w-8 h-8 rounded-lg flex-shrink-0 flex items-center justify-center text-white text-xs font-semibold bg-gradient-to-br from-primary to-violet-500 ml-2 shadow-sm self-start"
                >{{ userInitial }}</div>
              </div>
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

        <div class="border-t border-line-soft dark:border-gray-700/80 bg-white dark:bg-gray-900 px-5 py-3 flex-shrink-0">
          <div class="max-w-3xl mx-auto">
            <div class="border border-line dark:border-gray-700 rounded-xl overflow-hidden focus-within:border-primary focus-within:shadow-glow transition-all duration-150 bg-white dark:bg-gray-900">
              <textarea
                v-model="inputText"
                rows="3"
                placeholder="输入消息，Enter 发送 · Shift+Enter 换行 · @ 提及成员"
                class="w-full px-3.5 py-2.5 text-sm bg-transparent outline-none resize-none text-ink dark:text-gray-100 placeholder:text-ink-tertiary"
                @input="onInput"
                @keydown.enter.exact.prevent="send"
              />

              <AtMembersPopover
                :show="atShow"
                :keyword="atKeyword"
                :members="groupMembers"
                @pick="insertAt"
              />
              <div class="flex items-center justify-between px-2.5 py-1.5 border-t border-line-soft dark:border-gray-700 bg-surface-secondary/80 dark:bg-gray-800/40 relative">
                <div class="flex items-center gap-0.5">
                  <div class="relative">
                    <button
                      type="button"
                      class="ff-icon-btn w-7 h-7"
                      @click="showEmoji = !showEmoji"
                      @blur="closeEmojiLater"
                    >
                      <FaceSmileIcon class="w-4 h-4" />
                    </button>
                    <div
                      v-if="showEmoji"
                      class="absolute bottom-9 left-0 bg-white dark:bg-gray-800 border border-line dark:border-gray-700 rounded-xl p-2 shadow-float w-72 grid grid-cols-8 gap-1 z-20"
                      @click.stop
                    >
                      <button
                        v-for="e in emojis"
                        :key="e"
                        type="button"
                        class="w-8 h-8 rounded-md hover:bg-surface-secondary dark:hover:bg-gray-700 text-xl flex items-center justify-center transition-colors"
                        @click="insertEmoji(e)"
                      >{{ e }}</button>
                    </div>
                  </div>
                </div>
                <button
                  type="button"
                  class="ff-btn-primary h-7 px-4 text-sm"
                  :disabled="!inputText.trim()"
                  @click="send"
                >
                  发送
                </button>
              </div>
            </div>
          </div>
        </div>
      </template>

      <div v-else class="flex-1 flex flex-col items-center justify-center text-ink-tertiary select-none">
        <div class="w-20 h-20 rounded-2xl bg-primary-soft dark:bg-primary/10 flex items-center justify-center mb-4">
          <ChatBubbleLeftRightIcon class="w-10 h-10 text-primary/60" />
        </div>
        <p class="text-sm font-medium text-ink-secondary dark:text-gray-400">选择一个会话开始聊天</p>
        <p class="text-xs text-ink-tertiary mt-1.5">支持群聊、@ 提及、消息撤回与视频会议</p>
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
  <!-- 会议房间（Agora 声网） -->
  <MeetingRoom v-model:visible="showMeetingRoom" @ended="onMeetingEnded" />
</template>

<script setup>
defineOptions({ name: 'Messages' })

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


import { ref, computed, nextTick, watch, onMounted, onUnmounted, onBeforeUnmount } from 'vue'  // onUnmounted used inside onMounted
import { useVirtualList } from '@/composables/useVirtualList'
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
import MeetingRoom from '@/components/Meetings/MeetingRoom.vue'
import { useGroupStore } from '@/stores/group'
import { useMeetingStore } from '@/stores/meeting'
import { useAgora } from '@/composables/useAgora'
import { ElMessage } from '@/api/toast'
import { VideoCameraIcon } from '@heroicons/vue/24/outline'

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
const agora = useAgora()

async function onMeetingCreated(meeting) {
  // 创建后自动 join
  showCreateMeeting.value = false
  showMeetingCenter.value = false
  try {
    const joinPayload = await meetingStore.join(meeting.id)
    await agora.joinMeeting(joinPayload)
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
    await agora.joinMeeting(data)
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

/** 群会议入口：用 Agora 声网（替代 P2P mesh） */
async function onGroupVideoCall() {
  if (!activeConv.value || activeConv.value.type !== 'group') return
  try {
    // 1. 创建会议（同一个 channelName 会生成，固定）
    const meeting = await meetingStore.create({})
    if (!meeting?.id) return
    // 2. 邀请所有群成员（拉他们进同一个 Agora 频道）
    const memberIds = groupMembers.value.map(m => m.userId).filter(id => id && id !== userStore.userInfo?.id)
    if (memberIds.length > 0) {
      await meetingStore.inviteMembers(meeting.id, memberIds)
    }
    // 3. 创建者自动加入（获取 Agora token）
    const joinPayload = await meetingStore.join(meeting.id)
    // 4. 加入声网频道
    await agora.joinMeeting(joinPayload)
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
const showEmoji = ref(false)
const activeRecall = ref(null)
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

// ─── 虚拟滚动：会话列表 ───
const {
  containerRef: convListRef,
  totalHeight: convTotalHeight,
  offsetY: convOffsetY,
  visibleItems: convVisible,
  onScroll: onConvScroll,
  itemHeight: convItemHeight
} = useVirtualList(filteredConvs, { itemHeight: 68, overscan: 8 })

// ─── 虚拟滚动：消息列表 ───
const {
  containerRef: msgListRef,
  totalHeight: msgTotalHeight,
  offsetY: msgOffsetY,
  visibleItems: msgVisible,
  onScroll: onMsgScroll,
  scrollToBottom: virtualScrollBottom,
  itemHeight: msgItemHeight
} = useVirtualList(activeMessages, { itemHeight: 88, overscan: 10 })

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
  await messagesStore.sendMessage(text)
  scrollBottom()
}

async function doRecall(msgId) {
  await messagesStore.recallMessage(msgId)
  activeRecall.value = null
}

function scrollBottom() {
  virtualScrollBottom('auto')
}

watch(() => messagesStore.activeId, () => scrollBottom())
watch(() => activeMessages.value?.length, () => scrollBottom())

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
function onSignalRMeetingEnded() {
  showMeetingRoom.value = false
  // 触发当前 MeetingRoom 组件的 ended 事件清理
  if (meetingStore.current) {
    meetingStore.leave(meetingStore.current.id)
  }
}
window.addEventListener('meeting-ended', onSignalRMeetingEnded)
onUnmounted(() => {
  window.removeEventListener('meeting-ended', onSignalRMeetingEnded)
})

/* vClickOutside DISABLED for testing */
</script>