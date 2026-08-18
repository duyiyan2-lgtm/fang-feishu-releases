<template>
  <view class="im-container">
    <view class="message-tools">
      <view class="message-search">
        <text class="search-icon">🔍</text>
        <input
          v-model="searchText"
          class="search-input"
          placeholder="搜索消息、联系人"
          placeholder-class="placeholder"
          confirm-type="search"
          @confirm="handleSearch"
        />
      </view>
      <view class="message-create" @tap="handleCreate">
        <text class="create-icon">＋</text>
      </view>
    </view>

    <view class="message-tabs">
      <text
        class="message-tab"
        :class="{ active: tabFilter === 'all' }"
        @tap="tabFilter = 'all'"
      >全部</text>
      <text
        class="message-tab"
        :class="{ active: tabFilter === 'unread' }"
        @tap="tabFilter = 'unread'"
      >未读</text>
      <text
        class="message-tab"
        :class="{ active: tabFilter === 'mention' }"
        @tap="tabFilter = 'mention'"
      >@我</text>
    </view>

    <!-- 消息搜索结果 -->
    <view v-if="searching" class="conv-list">
      <view v-if="searchResults.length === 0" class="search-result-empty">
        <text class="search-result-empty-text">未找到相关消息</text>
      </view>
      <view
        v-for="(item, idx) in searchResults"
        :key="idx"
        class="conv-item search-result-item"
        @tap="openSearchResult(item)"
      >
        <view class="search-result-avatar" :style="{ backgroundColor: getAvatarColor(item.message?.conversationId || '0') }">
          <text class="conv-avatar-text">{{ (item.conversationTitle || '?')[0] }}</text>
        </view>
        <view class="conv-info">
          <view class="conv-top">
            <text class="conv-name">{{ item.conversationTitle || '未知会话' }}</text>
            <text class="conv-time">{{ formatTime(item.message?.createdAt) }}</text>
          </view>
          <view class="conv-bottom">
            <text class="search-result-sender">{{ item.message?.senderName }}：</text>
            <text class="search-result-content">{{ item.message?.content }}</text>
          </view>
        </view>
      </view>
      <view class="search-result-back" @tap="searching = false; searchText = ''">
        <text class="search-result-back-text">‹ 返回会话列表</text>
      </view>
    </view>

    <!-- 会话列表 -->
    <view v-if="!searching && conversations.length" class="conv-list">
      <view
        v-for="conv in conversations"
        :key="conv.id"
        class="conv-item"
        @tap="openChat(conv)"
        @longpress="confirmDeleteConv(conv)"
      >
        <view class="conv-avatar" :style="{ backgroundColor: getAvatarColor(getAvatarTarget(conv)) }">
          <text class="conv-avatar-text">{{ getConvName(conv)[0] || '?' }}</text>
        </view>
        <view class="conv-info">
          <view class="conv-top">
            <text class="conv-name">{{ getConvName(conv) }}</text>
            <text class="conv-time">{{ formatTime(conv.lastMessage?.createdAt) }}</text>
          </view>
          <view class="conv-bottom">
            <view class="conv-preview">{{ getConvPreview(conv) }}</view>
            <view v-if="conv.unreadCount > 0" class="conv-badge">
              <text class="conv-badge-text">{{ conv.unreadCount > 99 ? '99+' : conv.unreadCount }}</text>
            </view>
          </view>
        </view>
      </view>
    </view>

    <!-- 空状态 -->
    <view v-else-if="!searching" class="empty-state">
      <view class="empty-icon">💬</view>
      <text class="empty-text">暂无会话</text>
      <text class="empty-hint">开始您的第一次聊天吧</text>
    </view>

    <!-- ===== 创建群聊弹窗 ===== -->
    <view v-if="showGroupModal" class="modal-overlay" @tap="showGroupModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">创建群聊</text>

        <!-- 群名称 -->
        <view class="form-group">
          <text class="form-label">群名称</text>
          <input v-model="groupName" class="form-input" placeholder="输入群名称" />
        </view>

        <!-- 搜索联系人 -->
        <view class="form-group">
          <text class="form-label">选择成员（{{ selectedMembers.length }} 人）</text>
          <input v-model="memberSearch" class="form-input" placeholder="搜索联系人..." />
        </view>

        <!-- 联系人列表 -->
        <scroll-view class="member-scroll" scroll-y>
          <view
            v-for="m in filteredMembers"
            :key="m.id"
            class="member-row"
            @tap="toggleMember(m)"
          >
            <view class="member-avatar-sm" :style="{ backgroundColor: getAvatarColor(m.id) }">
              <text class="avatar-sm-text">{{ (m.realName || m.username)[0] }}</text>
            </view>
            <text class="member-row-name">{{ m.realName || m.username }}</text>
            <view class="member-check" :class="{ checked: selectedIds.has(m.id) }">
              <text v-if="selectedIds.has(m.id)" class="check-mark">✓</text>
            </view>
          </view>
          <view v-if="!filteredMembers.length" class="member-empty">无匹配成员</view>
        </scroll-view>

        <view class="modal-btns">
          <button class="modal-cancel" @tap="showGroupModal = false">取消</button>
          <button class="modal-confirm" :disabled="!canCreateGroup" @tap="createGroup">创建群聊</button>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onShow, onHide, onPullDownRefresh, onShareAppMessage } from '@dcloudio/uni-app'
import { getConversations, getMessages, createConversation, sendMessage, searchMessages } from '@/api/im'
import { get } from '@/api/request'
import { useAuthStore } from '@/stores/auth'
import { signalR } from '@/api/signalr'
import { updateBadges } from '@/utils/badge'

const authStore = useAuthStore()
const myId = authStore.userInfo?.id || ''

interface Conversation {
  id: string
  title?: string
  type: string
  members: Array<{ userId: string; realName: string }>
  lastMessage?: { id: string; content: string; createdAt: string; senderName: string; senderId: string; isRecalled?: boolean; mentionUserIds?: string[] }
  unreadCount: number
}

const allConversations = ref<Conversation[]>([]) // 完整的会话列表
const searchText = ref('')
const tabFilter = ref('all') // all / unread / mention
const initialLoadDone = ref(false) // 首次加载标记（用于控制 API @标记只补全一次）

// 搜索状态
const searchResults = ref<any[]>([])
const searching = ref(false)

async function handleSearch() {
  const kw = searchText.value.trim()
  if (!kw) {
    searching.value = false
    searchResults.value = []
    return
  }
  searching.value = true
  try {
    const res: any = await searchMessages(kw)
    searchResults.value = Array.isArray(res) ? res : []
  } catch {
    searchResults.value = []
  }
}

function openSearchResult(item: any) {
  const msg = item.message || item
  const title = item.conversationTitle || ''
  uni.navigateTo({
    url: `/pages/im/chat?conversationId=${msg.conversationId}&name=${encodeURIComponent(title)}`,
  })
}

/** 实时过滤的会话列表 */
const conversations = computed(() => {
  let list = allConversations.value

  // Tab 过滤
  if (tabFilter.value === 'unread') {
    list = list.filter((c) => c.unreadCount > 0)
  } else if (tabFilter.value === 'mention') {
    list = list.filter((c) => pendingMentionConvs.value.has(c.id))
  }

  // 搜索关键字过滤
  if (searchText.value.trim()) {
    const kw = searchText.value.trim().toLowerCase()
    list = list.filter((conv) => {
      const name = getConvName(conv).toLowerCase()
      const content = (conv.lastMessage?.content || '').toLowerCase()
      return name.includes(kw) || content.includes(kw)
    })
  }

  return list
})

async function loadConversations() {
  try {
    const res: any = await getConversations()
    const list = Array.isArray(res) ? res : res?.items || res?.list || []
    // 只显示有消息的会话
    let filtered = list.filter((c: any) => c.lastMessage !== null)
    // 同成员组合的私聊只保留最新的一个（避免之前遗留的重复会话）
    // 说明：现在 contacts.startChat 不会再创建新副本，
    // 所以这里的去重不会吃掉用户的新消息，只是清理历史数据
    const seen = new Map<string, any>()
    for (const c of filtered) {
      if (c.type === 'Private') {
        const key = c.members?.map((m: any) => m.userId).sort().join(',') || c.id
        const existing = seen.get(key)
        if (!existing || new Date(c.lastMessage.createdAt) > new Date(existing.lastMessage.createdAt)) {
          seen.set(key, c)
        }
      } else {
        seen.set(c.id, c)
      }
    }
    // 过滤掉用户隐藏的对话框（本地存储，不影响对方）
    const hidden = getHiddenConvs()
    allConversations.value = Array.from(seen.values()).filter(c => !hidden.has(c.id))

    // 仅在首次加载时从 API 数据补全@标记（之后用 SignalR + 持久化追踪，
    // 避免用户点进去清除后 onShow 重新加载又被标记回来）
    if (!initialLoadDone.value) {
      initialLoadDone.value = true
      // 从 API 最后一条消息标记@
      for (const c of allConversations.value) {
        if (c.type === 'Group' && c.lastMessage) {
          const hasMention = c.lastMessage.mentionUserIds?.includes(myId) ||
            isMentionedInContent(c.lastMessage.content)
          if (hasMention) addPendingMention(c.id)
        }
      }
      // 额外扫描：未读群聊的最近消息（应对@不在最后一条的情况）
      scanUnreadGroupMentions()
    }
  } catch {
    allConversations.value = []
  }
}

/** 加号按钮：新建会话 */
function handleCreate() {
  uni.showActionSheet({
    itemList: ['发起群聊', '跳转通讯录选人'],
    success: (res) => {
      if (res.tapIndex === 0) {
        openGroupModal()
      } else if (res.tapIndex === 1) {
        uni.switchTab({ url: '/pages/contacts/index' })
      }
    },
  })
}

// ======== 群聊创建 ========
const showGroupModal = ref(false)
const groupName = ref('')
const memberSearch = ref('')
const allMembers = ref<any[]>([])
const selectedIds = ref<Set<string>>(new Set())

const selectedMembers = computed(() => allMembers.value.filter((m) => selectedIds.value.has(m.id)))

const filteredMembers = computed(() => {
  let list = allMembers.value
  if (memberSearch.value.trim()) {
    const kw = memberSearch.value.trim().toLowerCase()
    list = list.filter((m) => (m.realName || m.username || '').toLowerCase().includes(kw))
  }
  return list
})

const canCreateGroup = computed(() => groupName.value.trim().length > 0 && selectedIds.value.size > 0)

async function openGroupModal() {
  showGroupModal.value = true
  groupName.value = ''
  memberSearch.value = ''
  selectedIds.value = new Set()

  // 加载联系人（排除自己 = 用户隔离）
  try {
    const res: any = await get('/contacts')
    const list = Array.isArray(res) ? res : []
    allMembers.value = list.filter((m: any) => m.id !== myId)
  } catch {
    allMembers.value = []
  }
}

function toggleMember(m: any) {
  if (selectedIds.value.has(m.id)) {
    selectedIds.value.delete(m.id)
  } else {
    selectedIds.value.add(m.id)
  }
  selectedIds.value = new Set(selectedIds.value) // 触发响应式
}

async function createGroup() {
  if (!canCreateGroup.value) return
  try {
    const conv = await createConversation({
      type: 'Group',
      title: groupName.value.trim(),
      memberUserIds: Array.from(selectedIds.value),
    })
    if (conv?.id) {
      // 发送系统入群消息（服务端广播给所有成员）
      const memberNames = selectedMembers.value.map(m => m.realName || m.username)
      try {
        await sendMessage({
          conversationId: conv.id,
          content: `__SYSTEM_GROUP_JOIN__:${memberNames.join(',')}:${groupName.value.trim()}`,
          messageType: 'System',
        })
      } catch {}
      // 本地缓存群主信息（后端不一定返回 ownerId）
      uni.setStorageSync(`group_owner_${conv.id}`, myId)
      uni.showToast({ title: '群聊创建成功', icon: 'success' })
      showGroupModal.value = false
      uni.navigateTo({
        url: `/pages/im/chat?conversationId=${conv.id}&name=${encodeURIComponent(groupName.value.trim())}&type=Group`,
      })
    }
  } catch {
    uni.showToast({ title: '创建失败', icon: 'none' })
  }
}
// ========

/** 本地存储键（按用户隔离，不影响其他人） */
const HIDDEN_KEY = `hidden_convs_${myId}`

/** 获取已隐藏的会话 ID 集合 */
function getHiddenConvs(): Set<string> {
  try {
    const raw = uni.getStorageSync(HIDDEN_KEY) || '[]'
    return new Set(JSON.parse(raw))
  } catch { return new Set() }
}

/** 保存隐藏的会话 ID */
function saveHiddenConvs(ids: Set<string>) {
  uni.setStorageSync(HIDDEN_KEY, JSON.stringify(Array.from(ids)))
}

/** 长按对话框 → 删除（仅本地隐藏，并标记删除时间戳以过滤旧消息） */
function confirmDeleteConv(conv: Conversation) {
  uni.showActionSheet({
    itemList: ['删除对话框'],
    success: (res) => {
      if (res.tapIndex === 0) {
        const hidden = getHiddenConvs()
        hidden.add(conv.id)
        saveHiddenConvs(hidden)
        // 保存删除时间戳，重新进入时只显示之后的消息
        const delKey = `del_at_${conv.id}_${myId}`
        uni.setStorageSync(delKey, new Date().toISOString())
        // 立即从列表移除
        allConversations.value = allConversations.value.filter(c => c.id !== conv.id)
        uni.showToast({ title: '已删除', icon: 'success' })
      }
    },
  })
}

/** 私聊取对方成员，群聊取 null */
function getOtherMember(conv: Conversation) {
  if (conv.type === 'Private' && conv.members?.length) {
    return conv.members.find((m) => m.userId !== myId)
  }
  return null
}

function getConvName(conv: Conversation): string {
  const other = getOtherMember(conv)
  if (other) return other.realName
  if (conv.title) return conv.title
  if (conv.members?.length) {
    return conv.members.map((m) => m.realName).join('、')
  }
  return '未知会话'
}

/** 持久化存储：哪些会话有未读的@消息（跨会话持久，点进去才清除） */
const PENDING_MENTION_KEY = `pending_mentions_${myId}`

function loadPendingMentions(): Set<string> {
  try {
    const raw = uni.getStorageSync(PENDING_MENTION_KEY) || '[]'
    return new Set(JSON.parse(raw))
  } catch { return new Set() }
}

function savePendingMentions(set: Set<string>) {
  uni.setStorageSync(PENDING_MENTION_KEY, JSON.stringify(Array.from(set)))
}

const pendingMentionConvs = ref<Set<string>>(loadPendingMentions())

function addPendingMention(convId: string) {
  if (!convId) return
  pendingMentionConvs.value.add(convId)
  pendingMentionConvs.value = new Set(pendingMentionConvs.value)
  savePendingMentions(pendingMentionConvs.value)
}

function clearPendingMention(convId: string) {
  if (!convId) return
  pendingMentionConvs.value.delete(convId)
  pendingMentionConvs.value = new Set(pendingMentionConvs.value)
  savePendingMentions(pendingMentionConvs.value)
}

/** 检查消息内容是否@了当前用户（兜底） */
function isMentionedInContent(content: string): boolean {
  if (!content) return false
  const myName = authStore.userInfo?.realName || authStore.userInfo?.username || ''
  if (!myName) return false
  const searchStr = `@${myName}`
  let idx = 0
  while ((idx = content.indexOf(searchStr, idx)) !== -1) {
    const after = content[idx + searchStr.length] || ''
    if (!after || /[\s，。、！？；：,\.!?;:)\]>}]/.test(after)) return true
    idx++
  }
  return false
}

/** 扫描未读群聊的最近消息，查找@提醒（解决@不在最后一条时检测不到的问题） */
async function scanUnreadGroupMentions() {
  const toScan = allConversations.value.filter(
    c => c.type === 'Group' && c.unreadCount > 0 && !pendingMentionConvs.value.has(c.id)
  )
  if (toScan.length === 0) return
  // 分批扫描，最多查3个群聊，避免请求太多
  const batch = toScan.slice(0, 3)
  for (const c of batch) {
    try {
      const res: any = await getMessages(c.id, 1, 20)
      const msgs = Array.isArray(res) ? res : res?.items || res?.list || []
      for (const msg of msgs) {
        if (msg.mentionUserIds?.includes(myId) || isMentionedInContent(msg.content)) {
          addPendingMention(c.id)
          break
        }
      }
    } catch { /* 单个群聊失败不影响其他 */ }
  }
}

/** 会话预览：先检查 API 返回的 isRecalled 标记，再检查系统消息 */
function getConvPreview(conv: Conversation): string {
  if (!conv.lastMessage) return '暂无消息'
  if (conv.lastMessage.isRecalled) {
    return conv.lastMessage.senderId === myId ? '你撤回了一条消息' : '对方撤回了一条消息'
  }
  const content = conv.lastMessage.content || ''
  let preview: string
  if (conv.lastMessage.fileId) {
    preview = isImageFile(conv.lastMessage.fileName) ? '[图片]' : '[文件]'
  } else if (content.startsWith('__SYSTEM_GROUP_JOIN__')) {
    preview = '[系统消息]'
  } else {
    preview = content.replace(/\n/g, ' ').replace(/\r/g, '')
  }
  // 群聊中有未读的@提醒（持久化追踪，不只是最后一条消息）
  if (conv.type === 'Group' && pendingMentionConvs.value.has(conv.id)) {
    preview = `【你已被@】${preview}`
  }
  return preview
}

/** 私聊取对方 ID 做头像颜色，群聊用会话 ID */
function getAvatarTarget(conv: Conversation): string {
  const other = getOtherMember(conv)
  return other?.userId || conv.id
}

function formatTime(timeStr: string): string {
  if (!timeStr) return ''
  const d = new Date(timeStr)
  const now = new Date()
  const isToday = d.toDateString() === now.toDateString()
  if (isToday) return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
  return `${d.getMonth() + 1}/${d.getDate()}`
}

function openChat(conv: Conversation) {
  // 点进会话 → 清除所有未读@标记（含持久化存储）
  if (conv.type === 'Group') clearPendingMention(conv.id)
  uni.navigateTo({ url: `/pages/im/chat?conversationId=${conv.id}&name=${encodeURIComponent(getConvName(conv))}&type=${conv.type}` })
}

/** 判断文件名是否为图片 */
function isImageFile(fileName: string): boolean {
  return /\.(jpg|jpeg|png|gif|webp|bmp)$/i.test(fileName || '')
}

const colors = ['#409EFF', '#67C23A', '#E6A23C', '#F56C6C', '#909399', '#B37FEB', '#00BFA5', '#FF7043']
function getAvatarColor(id: string): string {
  let hash = 0
  for (let i = 0; i < id.length; i++) hash = ((hash << 5) - hash) + id.charCodeAt(i)
  return colors[Math.abs(hash) % colors.length]
}

/** SignalR 消息回调 */
let signalRHandler: ((raw: any) => void) | null = null

onShow(() => {
  loadConversations()
  updateBadges()

  // 注册 SignalR 监听 → 实时更新会话列表
  signalRHandler = (raw: any) => {
    if (raw.type !== 1 || !raw.target) return

    // 收到新消息 → 更新对应会话的最后一条消息并置顶
    if (raw.target === 'ReceiveMessage') {
      const msg = raw.arguments?.[0]
      if (!msg?.conversationId) return

      // 用展开语法创建新数组，确保响应式更新
      const list = [...allConversations.value]
      const idx = list.findIndex(c => c.id === msg.conversationId)
      if (idx !== -1) {
        // 更新会话的最后一条消息和未读数
        const updated = { ...list[idx] }
        // SignalR 推送不包含 senderName/fileName，用内容做文件名回退
        const isImage = msg.fileId && /\.(jpg|jpeg|png|gif|webp|bmp)$/i.test(msg.content || '')
        updated.lastMessage = {
          id: msg.id,
          content: msg.content || '',
          createdAt: msg.createdAt,
          senderName: msg.senderName || '',
          senderId: msg.senderId || '',
          isRecalled: msg.isRecalled || false,
          fileId: msg.fileId || null,
          fileName: msg.fileName || (isImage ? msg.content : null),
          mentionUserIds: msg.mentionUserIds || [],
        }
        const isSelf = msg.senderId === myId
        updated.unreadCount = isSelf ? (updated.unreadCount || 0) : (updated.unreadCount || 0) + 1
        // 新消息@了我 → 标记为未读@（持久化，不限于最后一条消息）
        if (updated.type === 'Group' && !isSelf) {
          const hasMention = msg.mentionUserIds?.includes(myId) || isMentionedInContent(msg.content)
          if (hasMention) addPendingMention(msg.conversationId)
        }
        list.splice(idx, 1)   // 从原位置移除
        list.unshift(updated) // 置顶
        allConversations.value = list
        updateBadges()
      } else {
        // 全新会话 → 先标记@通知（避免 loadConversations 因 initialLoadDone 跳过）
        if (msg.mentionUserIds?.includes(myId) || isMentionedInContent(msg.content)) {
          addPendingMention(msg.conversationId)
        }
        // 重新加载列表
        loadConversations()
      }
      return
    }

    // 消息被撤回 → 更新预览文字
    if (raw.target === 'MessageRecalled') {
      const data = raw.arguments?.[0]
      if (!data?.conversationId) return

      const list = [...allConversations.value]
      const idx = list.findIndex(c => c.id === data.conversationId)
      if (idx !== -1 && list[idx].lastMessage?.id === data.id) {
        const updated = { ...list[idx] }
        updated.lastMessage = { ...updated.lastMessage!, isRecalled: true }
        list[idx] = updated
        allConversations.value = list
      }
    }
  }
  signalR.onMessage(signalRHandler)
})

/** 离开页面时清理 SignalR 监听，防止重复注册 */
onHide(() => {
  if (signalRHandler) {
    signalR.offMessage(signalRHandler)
    signalRHandler = null
  }
})

/** 下拉刷新 */
onPullDownRefresh(() => {
  loadConversations()
  updateBadges()
  uni.stopPullDownRefresh()
})

/** 右上角分享 */
onShareAppMessage(() => {
  return {
    title: '仿飞书 - 消息',
    path: '/pages/im/index',
  }
})
</script>

<style scoped>
.im-container {
  min-height: 100vh;
  background: #f6f8fc;
  padding: 24rpx;
  box-sizing: border-box;
}
.message-tools {
  display: flex;
  align-items: center;
  gap: 16rpx;
  margin-bottom: 18rpx;
}
.message-search {
  flex: 1;
  height: 76rpx;
  padding: 0 24rpx;
  border-radius: 24rpx;
  background: #fff;
  border: 1rpx solid #edf1f7;
  box-shadow: 0 10rpx 28rpx rgba(31, 49, 84, 0.06);
  display: flex;
  align-items: center;
}
.search-icon {
  font-size: 26rpx;
  margin-right: 10rpx;
  flex-shrink: 0;
}
.search-input {
  flex: 1;
  font-size: 26rpx;
  color: #111827;
  height: 40rpx;
}
.search-text {
  font-size: 26rpx;
  color: #a8b0c2;
}
.message-create {
  width: 76rpx;
  height: 76rpx;
  border-radius: 24rpx;
  background: #1f6fff;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 12rpx 28rpx rgba(31, 111, 255, 0.2);
}
.create-icon {
  color: #fff;
  font-size: 38rpx;
  font-weight: 700;
  line-height: 1;
}
.message-tabs {
  display: flex;
  gap: 14rpx;
  margin-bottom: 18rpx;
}
.message-tab {
  padding: 10rpx 26rpx;
  border-radius: 999rpx;
  background: #fff;
  color: #64748b;
  font-size: 25rpx;
  box-shadow: 0 8rpx 22rpx rgba(31, 49, 84, 0.05);
}
.message-tab.active {
  background: #eef4ff;
  color: #1f6fff;
  font-weight: 700;
}
.conv-list {
  background: #fff;
  border-radius: 28rpx;
  overflow: hidden;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.07);
}
.conv-item {
  display: flex;
  align-items: center;
  padding: 26rpx 28rpx;
  border-bottom: 1rpx solid #f0f2f5;
}
.conv-item:active {
  background: #f8fbff;
}
.conv-avatar {
  width: 92rpx;
  height: 92rpx;
  border-radius: 28rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 20rpx;
  flex-shrink: 0;
}
.conv-avatar-text {
  color: #fff;
  font-size: 34rpx;
  font-weight: 700;
}
.conv-info {
  flex: 1;
  min-width: 0;
}
.conv-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 6rpx;
}
.conv-name {
  font-size: 30rpx;
  font-weight: 700;
  color: #111827;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.conv-time {
  font-size: 22rpx;
  color: #a8b0c2;
  flex-shrink: 0;
  margin-left: 16rpx;
}
.conv-bottom {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.conv-preview {
  font-size: 24rpx;
  color: #7b8494;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.conv-badge {
  min-width: 32rpx;
  height: 32rpx;
  background: #ff4d4f;
  border-radius: 16rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 8rpx;
  margin-left: 12rpx;
}
.conv-badge-text {
  color: #fff;
  font-size: 20rpx;
  font-weight: 600;
}
.empty-state {
  margin-top: 28rpx;
  padding: 130rpx 0;
  text-align: center;
  background: #fff;
  border-radius: 28rpx;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.06);
}
.empty-icon {
  font-size: 76rpx;
  margin-bottom: 16rpx;
}
.empty-text {
  font-size: 28rpx;
  color: #64748b;
  display: block;
}
.empty-hint {
  font-size: 24rpx;
  color: #a8b0c2;
  margin-top: 8rpx;
  display: block;
}

/* ===== 创建群聊弹窗 ===== */
.modal-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0,0,0,0.45);
  z-index: 999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 60rpx;
}
.modal-popup {
  width: 100%;
  max-width: 580rpx;
  max-height: 80vh;
  background: #fff;
  border-radius: 28rpx;
  padding: 32rpx;
  display: flex;
  flex-direction: column;
}
.modal-title {
  font-size: 34rpx;
  font-weight: 700;
  text-align: center;
  display: block;
  margin-bottom: 24rpx;
  color: #111827;
}
.form-group { margin-bottom: 20rpx; }
.form-label {
  font-size: 24rpx;
  color: #64748b;
  display: block;
  margin-bottom: 8rpx;
}
.form-input {
  height: 72rpx;
  background: #f6f8fc;
  border-radius: 16rpx;
  padding: 0 20rpx;
  font-size: 26rpx;
  color: #111827;
  border: 1rpx solid #edf1f7;
  box-sizing: border-box;
}
.member-scroll {
  max-height: 400rpx;
  margin-bottom: 20rpx;
  border: 1rpx solid #edf1f7;
  border-radius: 16rpx;
  background: #fafbfc;
}
.member-row {
  display: flex;
  align-items: center;
  padding: 16rpx 20rpx;
  border-bottom: 1rpx solid #f0f2f5;
}
.member-row:active { background: #eef4ff; }
.member-row:last-child { border-bottom: none; }
.member-avatar-sm {
  width: 48rpx;
  height: 48rpx;
  border-radius: 12rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 16rpx;
  flex-shrink: 0;
}
.avatar-sm-text { color: #fff; font-size: 22rpx; font-weight: 600; }
.member-row-name {
  flex: 1;
  font-size: 26rpx;
  color: #111827;
}
.member-check {
  width: 34rpx;
  height: 34rpx;
  border: 2rpx solid #cfd6e3;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.member-check.checked { background: #1f6fff; border-color: #1f6fff; }
.check-mark { color: #fff; font-size: 20rpx; font-weight: bold; }
.member-empty {
  text-align: center;
  padding: 40rpx 0;
  font-size: 24rpx;
  color: #a8b0c2;
}
.modal-btns { display: flex; gap: 20rpx; margin-top: 8rpx; }
.modal-cancel, .modal-confirm {
  flex: 1;
  height: 76rpx;
  line-height: 76rpx;
  font-size: 28rpx;
  border-radius: 20rpx;
  border: none;
  text-align: center;
}
.modal-cancel { background: #f6f8fc; color: #374151; }
.modal-confirm { background: #1f6fff; color: #fff; }
.modal-confirm[disabled] { opacity: 0.4; }

/* ===== 消息搜索结果 ===== */
.search-result-item { flex-direction: row; }
.search-result-avatar {
  width: 64rpx; height: 64rpx;
  border-radius: 18rpx;
  display: flex; align-items: center; justify-content: center;
  margin-right: 16rpx; flex-shrink: 0;
}
.search-result-avatar .conv-avatar-text { color: #fff; font-size: 26rpx; font-weight: 600; }
.search-result-sender {
  font-size: 22rpx; color: #1f6fff; flex-shrink: 0;
}
.search-result-content {
  font-size: 24rpx; color: #7b8494;
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.search-result-empty {
  padding: 80rpx 0; text-align: center;
}
.search-result-empty-text { font-size: 26rpx; color: #a8b0c2; }
.search-result-back {
  padding: 24rpx; text-align: center; border-top: 1rpx solid #f0f2f5;
}
.search-result-back:active { background: #f8fbff; }
.search-result-back-text { font-size: 26rpx; color: #1f6fff; }
</style>
