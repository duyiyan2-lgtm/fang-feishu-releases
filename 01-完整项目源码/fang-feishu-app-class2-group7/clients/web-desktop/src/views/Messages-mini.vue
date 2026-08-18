<template><div>ok</div></template>

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


import { ref, computed, nextTick, watch, onMounted } from 'vue'
// import { useMessagesStore } from '@/stores/messages'
// import { useUserStore } from '@/stores/user'
// import {
  MagnifyingGlassIcon, ChatBubbleLeftRightIcon, EllipsisHorizontalIcon,
  FaceSmileIcon, UserGroupIcon, UserPlusIcon, ArrowUturnLeftIcon
} from '@heroicons/vue/24/outline'
// import CreateGroupDialog from '@/components/CreateGroupDialog.vue'
// import GroupPanel from '@/components/GroupPanel.vue'
// import AtMembersPopover from '@/components/AtMembersPopover.vue'
// import CreateMeetingDialog from '@/components/Meetings/CreateMeetingDialog.vue'
// import MeetingRoom from '@/components/Meetings/MeetingRoom.vue'
// import { useGroupStore } from '@/stores/group'
// import { useMeetingStore } from '@/stores/meeting'
// import { useAgora } from '@/composables/useAgora'
// import { VideoCameraIcon } from '@heroicons/vue/24/outline'

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
</script>