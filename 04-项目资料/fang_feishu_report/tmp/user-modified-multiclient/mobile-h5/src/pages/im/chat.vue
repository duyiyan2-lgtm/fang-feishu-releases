<template>
  <view class="chat-container">
    <!-- 群聊信息头（仅群聊显示，点击进入群管理） -->
    <view v-if="convType === 'Group'" class="group-info-bar" @tap="goGroupManage">
      <view class="group-info-avatar" :style="{ backgroundColor: getAvatarColor(conversationId) }">
        <text class="group-info-avatar-text">{{ (convName || '群')[0] }}</text>
      </view>
      <view class="group-info-text">
        <text class="group-info-name">{{ convName || '群聊' }}</text>
        <text class="group-info-hint">点击查看群成员和管理设置 ›</text>
      </view>
    </view>

    <!-- 群公告横幅（群聊且有公告时显示，可关闭） -->
    <view v-if="announcement && convType === 'Group'" class="announce-banner" @tap="showAnnouncePopup = true">
      <text class="announce-banner-icon">📢</text>
      <text class="announce-banner-text">{{ announcement }}</text>
      <text class="announce-banner-close" @tap.stop="announcement = ''">✕</text>
    </view>

    <!-- 群公告弹窗 -->
    <view v-if="showAnnouncePopup" class="announce-overlay" @tap="showAnnouncePopup = false">
      <view class="announce-popup" @tap.stop>
        <text class="announce-popup-title">📢 群公告</text>
        <scroll-view class="announce-popup-content" scroll-y>
          <text selectable>{{ announcement }}</text>
        </scroll-view>
        <button class="announce-popup-close" @tap="showAnnouncePopup = false">知道了</button>
      </view>
    </view>

    <!-- 消息列表 -->
    <scroll-view class="msg-list" scroll-y :scroll-top="scrollTop" @scrolltoupper="loadMore">
      <template v-for="item in displayItems" :key="item.type === 'separator' ? 'date:' + item.label : item.data.id">
        <!-- 日期分隔栏 -->
        <view v-if="item.type === 'separator'" class="date-sep-bar">
          <text class="date-sep-text">{{ item.label }}</text>
        </view>
        <!-- 消息体 -->
        <view
          v-else
          class="msg-item"
          :class="{ 'msg-self': item.data.senderId === myId && !item.data.isRecalled }"
          @touchstart="onLongPressStart(item.data, $event)"
          @touchend="onLongPressEnd($event)"
          @touchcancel="onLongPressEnd($event)"
          @mousedown="onLongPressStart(item.data, $event)"
          @mouseup="onLongPressEnd($event)"
          @mouseleave="onLongPressEnd($event)"
        >
          <!-- 已撤回消息 → 系统提示小字（类似微信） -->
          <template v-if="item.data.isRecalled">
            <view class="recalled-line">
              <text selectable class="recalled-text">{{ item.data.senderId === myId ? '你撤回了一条消息' : '对方撤回了一条消息' }}</text>
            </view>
          </template>
          <!-- 系统消息（群聊加入提示等）→ 同撤回风格 -->
          <template v-else-if="isSystemMsg(item.data)">
            <view class="recalled-line">
              <text selectable class="recalled-text">{{ formatSystemMsg(item.data) }}</text>
            </view>
          </template>
          <!-- 文件/图片消息（有 fileId 即为附件消息） -->
          <template v-else-if="item.data.fileId">
            <view class="msg-avatar" :style="{ backgroundColor: getAvatarColor(item.data.senderId) }">
              <text class="msg-avatar-text">{{ (item.data.senderName || '?')[0] }}</text>
            </view>
            <view class="msg-body">
              <view class="msg-sender">{{ item.data.senderName || '未知' }}</view>
              <!-- 图片：根据扩展名判断 -->
              <view v-if="isImageFile(item.data.fileName)" class="msg-bubble img-bubble" :class="{ 'msg-bubble-self': item.data.senderId === myId }">
                <image class="msg-image" :src="imageCache[item.data.fileId] || ''" mode="widthFix" @tap="previewFile(item.data.fileId)" />
              </view>
              <!-- 普通文件：显示文件名 + 下载按钮 -->
              <view v-else class="msg-bubble" :class="{ 'msg-bubble-self': item.data.senderId === myId }">
                <text selectable class="file-name">📎 {{ item.data.fileName || '附件' }}</text>
                <text class="file-download" @tap.stop="downloadFile(item.data.fileId, item.data.fileName)">下载</text>
              </view>
              <view class="msg-meta">
                <text class="msg-time">{{ formatTime(item.data.createdAt) }}</text>
                <!-- 表情回复（紧贴时间右侧） -->
                <view v-if="getGroupedReactions(item.data).length" class="msg-reactions" @tap.stop>
                  <text
                    v-for="reaction in getGroupedReactions(item.data)"
                    :key="reaction.type"
                    class="reaction-item"
                    :class="{ 'reaction-self': reaction.self }"
                    @tap="toggleReaction(item.data, reaction.type)"
                  >{{ reaction.type }}<text class="reaction-count">{{ reaction.count }}</text></text>
                </view>
              </view>
            </view>
          </template>
          <!-- 文字消息 -->
          <template v-else>
            <view class="msg-avatar" :style="{ backgroundColor: getAvatarColor(item.data.senderId) }">
              <text class="msg-avatar-text">{{ (item.data.senderName || '?')[0] }}</text>
            </view>
            <view class="msg-body">
              <view class="msg-sender">{{ item.data.senderName || '未知' }}</view>
              <view class="msg-bubble" :class="{ 'msg-bubble-self': item.data.senderId === myId }">
                <text selectable class="msg-text">
                  <text v-for="(seg, i) in parseMentions(item.data)" :key="i" :class="seg.isMention ? 'msg-mention' : ''">{{ seg.text }}</text>
                </text>
              </view>
              <view class="msg-meta">
                <text class="msg-time">{{ formatTime(item.data.createdAt) }}</text>
                <!-- 表情回复（紧贴时间右侧） -->
                <view v-if="getGroupedReactions(item.data).length" class="msg-reactions" @tap.stop>
                  <text
                    v-for="reaction in getGroupedReactions(item.data)"
                    :key="reaction.type"
                    class="reaction-item"
                    :class="{ 'reaction-self': reaction.self }"
                    @tap="toggleReaction(item.data, reaction.type)"
                  >{{ reaction.type }}<text class="reaction-count">{{ reaction.count }}</text></text>
                </view>
              </view>
            </view>
          </template>
        </view>
      </template>
      <!-- 底部锚点 -->
      <view id="bottom-anchor" style="height:2rpx" />
    </scroll-view>

    <!-- 表情回复选择器（浮动在输入区上方） -->
    <view v-if="showReactionPicker" class="reaction-picker-overlay" @tap="showReactionPicker = false">
      <view class="reaction-picker" @tap.stop>
        <text class="reaction-picker-title">选择表情回复</text>
        <view class="reaction-picker-grid">
          <text
            v-for="emoji in reactionEmojiList"
            :key="emoji"
            class="reaction-picker-emoji"
            @tap="selectReaction(emoji)"
          >{{ emoji }}</text>
        </view>
      </view>
    </view>

    <!-- @ 提及选择器 -->
    <view v-if="showMentionPicker" class="mention-overlay" @tap="showMentionPicker = false">
      <view class="mention-picker" @tap.stop>
        <text class="mention-picker-title">选择提醒成员</text>
        <input v-model="mentionSearch" class="mention-search-input" placeholder="搜索成员..." @input="onMentionSearchInput" />
        <scroll-view class="mention-scroll" scroll-y>
          <view
            v-for="m in mentionCandidates"
            :key="m.userId"
            class="mention-row"
            :class="{ selected: mentionSelectedIds.has(m.userId) }"
            @tap="toggleMentionMember(m)"
          >
            <view class="mention-avatar" :style="{ backgroundColor: getAvatarColor(m.userId) }">
              <text class="mention-avatar-text">{{ (m.realName || '?')[0] }}</text>
            </view>
            <text class="mention-name">{{ m.realName || m.username }}</text>
            <view class="mention-check" :class="{ checked: mentionSelectedIds.has(m.userId) }">
              <text v-if="mentionSelectedIds.has(m.userId)" class="mention-check-mark">✓</text>
            </view>
          </view>
          <view v-if="mentionCandidates.length === 0" class="mention-empty">无匹配成员</view>
        </scroll-view>
        <view class="mention-btns">
          <button class="mention-cancel" @tap="showMentionPicker = false">取消</button>
          <button class="mention-confirm" :disabled="mentionSelectedIds.size === 0" @tap="confirmMention">确定 ({{ mentionSelectedIds.size }})</button>
        </view>
      </view>
    </view>

    <!-- 底部输入区（输入栏 + emoji 面板） -->
    <view class="input-area">
      <!-- Emoji 选择面板（在输入栏上方） -->
      <view v-if="showEmoji" class="emoji-panel">
        <view class="emoji-grid">
          <text
            v-for="emoji in emojiList"
            :key="emoji"
            class="emoji-item"
            @tap="insertEmoji(emoji)"
          >{{ emoji }}</text>
        </view>
      </view>
      <view class="input-bar">
	        <!-- 已选的 @ 提及成员 -->
	        <view v-if="mentionMembers.length" class="mention-chips">
	          <text v-for="(m, i) in mentionMembers" :key="m.userId" class="mention-chip">@{{ m.realName || m.username }}<text class="mention-chip-remove" @tap="removeMention(i)">✕</text></text>
	        </view>
	        <text class="input-emoji" @tap="toggleEmojiPicker">😊</text>
	        <text class="input-at" @tap="openMentionPicker" v-if="convType === 'Group'">@</text>
	        <text class="input-attach" @tap="showAttachMenu">📎</text>
        <textarea
          v-model="inputText"
          class="input-field"
          placeholder="输入消息..."
          placeholder-class="placeholder"
          auto-height
          :maxlength="-1"
          @keydown="handleInputKeydown"
        />
        <button class="send-btn" :disabled="!inputText.trim()" @tap="handleSend">发送</button>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed, nextTick } from 'vue'
import { onLoad, onUnload, onHide } from '@dcloudio/uni-app'
import { getMessages, sendMessage, addReaction, removeReaction, getConversation, getAnnouncement } from '@/api/im'
import { useAuthStore } from '@/stores/auth'
import { patch, BASE_URL } from '@/api/request'
import { signalR } from '@/api/signalr'
import { uploadFile } from '@/api/drive'

const authStore = useAuthStore()
const myId = authStore.userInfo?.id || ''

const conversationId = ref('')
const convType = ref('')
const convName = ref('')
const messages = ref<any[]>([])
const inputText = ref('')
const scrollTop = ref(0)
/** 图片缓存：fileId → 临时路径（用 Authorization header 下载后缓存） */
const imageCache = ref<Record<string, string>>({})

/** 群公告 */
const announcement = ref('')
const showAnnouncePopup = ref(false)

/** 下载图片到临时路径（带 Auth header）并缓存 */
function loadImage(fileId: string): Promise<string> {
  if (imageCache.value[fileId]) return Promise.resolve(imageCache.value[fileId])
  return new Promise((resolve) => {
    const token = uni.getStorageSync('token') || ''
    uni.downloadFile({
      url: `${BASE_URL}/files/${fileId}/download`,
      header: { Authorization: `Bearer ${token}` },
      success: (res) => {
        if (res.statusCode === 200) {
          imageCache.value[fileId] = res.tempFilePath
          resolve(res.tempFilePath)
        } else {
          resolve('')
        }
      },
      fail: () => resolve(''),
    })
  })
}
const page = ref(1)
const hasMore = ref(true)
const sending = ref(false) // 防止重复发送

/** 强制滚动到底部 */
function scrollToBottom() {
  nextTick(() => {
    // 用不断增大的值确保每次都触发滚动
    scrollTop.value = Date.now()
  })
}

/** 加载消息 */
function loadMessages(reset = false) {
  if (reset) { page.value = 1; hasMore.value = true; messages.value = [] }
  if (!hasMore.value) return
  getMessages(conversationId.value, page.value, 20).then((res: any) => {
    const list = Array.isArray(res) ? res : res?.items || res?.list || []
    if (list.length < 20) hasMore.value = false
    // 如果该会话被删除过，只显示删除之后的消息（对方不受影响）
    const delKey = `del_at_${conversationId.value}_${myId}`
    let delTime: string | null = null
    try { delTime = uni.getStorageSync(delKey) || null } catch {}
    const filtered = delTime
      ? list.filter((msg: any) => msg.createdAt > delTime)
      : list
    // 如果原始有数据但全被过滤了，说明没有删除后的新消息了
    if (delTime && list.length > 0 && filtered.length === 0) {
      hasMore.value = false
    }
    // API 返回已经是正序（旧→新），无需 reverse
    // 首次加载直接赋值，加载更多（向上滚动→更旧消息）在前面插入
    if (page.value === 1) {
      messages.value = filtered
    } else {
      messages.value.unshift(...filtered)
    }
    page.value++
    scrollToBottom()
    // 加载图片消息的临时路径（带 Auth header 下载到本地才能显示）
    filtered.forEach((msg: any) => {
      if (msg.fileId && isImageFile(msg.fileName)) loadImage(msg.fileId)
    })
  }).catch(() => {})
}

function loadMore() {
  if (!hasMore.value) return
  loadMessages()
}

/** @提及 状态 */
const showMentionPicker = ref(false)
const mentionSearch = ref('')
function onMentionSearchInput(e: any) {
  // uni-app <input> 的 @input 事件：e.detail.value
  const val = e?.detail?.value ?? e?.target?.value ?? e ?? ''
  mentionSearch.value = val
}
const groupMembers = ref<any[]>([])
const mentionSelectedIds = ref<Set<string>>(new Set())
const mentionMembers = ref<any[]>([])

const mentionCandidates = computed(() => {
  let list = groupMembers.value.filter((m: any) => m.userId !== myId)
  if (mentionSearch.value.trim()) {
    const kw = mentionSearch.value.trim().toLowerCase()
    list = list.filter((m: any) => (m.realName || m.username || '').toLowerCase().includes(kw))
  }
  return list
})

async function loadGroupMembers() {
  if (convType.value !== 'Group' || !conversationId.value) return
  try {
    const conv: any = await getConversation(conversationId.value)
    if (conv?.members && Array.isArray(conv.members)) {
      groupMembers.value = conv.members
      console.log('[Mention] loaded', conv.members.length, 'members')
    } else {
      console.warn('[Mention] no members in response', conv)
      groupMembers.value = []
    }
  } catch (e) {
    console.warn('[Mention] failed to load members', e)
    groupMembers.value = []
  }
}

function openMentionPicker() {
  mentionSearch.value = ''
  mentionSelectedIds.value = new Set(mentionMembers.value.map((m: any) => m.userId))
  showMentionPicker.value = true
}

function toggleMentionMember(m: any) {
  if (mentionSelectedIds.value.has(m.userId)) {
    mentionSelectedIds.value.delete(m.userId)
  } else {
    mentionSelectedIds.value.add(m.userId)
  }
  mentionSelectedIds.value = new Set(mentionSelectedIds.value)
}

function confirmMention() {
  const selected = groupMembers.value.filter((m: any) => mentionSelectedIds.value.has(m.userId))
  mentionMembers.value = selected
  showMentionPicker.value = false
  // 在输入框追加 @ 提醒文字
  if (selected.length > 0) {
    const names = selected.map((m: any) => `@${m.realName || m.username}`).join(' ')
    inputText.value = inputText.value ? `${inputText.value} ${names} ` : `${names} `
  }
}

function removeMention(index: number) {
  mentionMembers.value.splice(index, 1)
}

/** 发送消息（乐观更新，成功后用真实 ID 替换临时 ID） */
async function handleSend() {
  const content = inputText.value.trim()
  if (!content || sending.value) return
  sending.value = true
  showEmoji.value = false  // 发送后收起表情面板
  showMentionPicker.value = false

  // 乐观更新：立即将消息显示到列表
  const tempId = `temp-${Date.now()}`
  const tempMsg: any = {
    id: tempId,
    content,
    messageType: 'Text',
    senderId: myId,
    senderName: authStore.userInfo?.realName || '我',
    createdAt: new Date().toISOString(),
    isRecalled: false, // 确保响应式系统能追踪后续变化
  }
  messages.value.push(tempMsg)
  inputText.value = ''

  // 滚动到底部
  scrollToBottom()

  try {
    const mentionUserIds = mentionMembers.value.length > 0
      ? mentionMembers.value.map((m: any) => m.userId)
      : undefined
    const result = await sendMessage({
      conversationId: conversationId.value,
      content,
      mentionUserIds,
    })
    mentionMembers.value = []
    clearDraft() // 发送成功，清除草稿
    // 用服务器返回的真实数据替换临时消息（确保撤回使用真实 ID）
    if (result) {
      const idx = messages.value.findIndex(m => m.id === tempId)
      if (idx !== -1) {
        // SignalR 可能已经先到了并添加了真实消息，此时 temp 已被移除
        Object.assign(messages.value[idx], result)
      }
    }
  } catch {
    // 发送失败，移除临时消息
    messages.value = messages.value.filter(m => m.id !== tempId)
    uni.showToast({ title: '发送失败', icon: 'none' })
  } finally {
    sending.value = false
  }
}

/** 长按消息 → 弹框选择撤回/表情回复 */
const pendingReactionMsg = ref<any>(null)
const showReactionPicker = ref(false)

function showMsgActions(msg: any) {
  if (msg.isRecalled) return
  const items: string[] = ['表情回复']
  if (msg.senderId === myId) items.push('撤回消息')
  uni.showActionSheet({
    itemList: items,
    success: (res) => {
      if (items[res.tapIndex] === '表情回复') {
        pendingReactionMsg.value = msg
        showReactionPicker.value = true
      } else if (items[res.tapIndex] === '撤回消息') {
        recallMessage(msg)
      }
    },
  })
}

const reactionEmojiList = [
  // 👍 手势
  '👍', '👎', '👌', '✌️', '🤞', '🤟', '🤘', '🤙',
  '👋', '🤚', '✋', '🖐️', '👐', '🙌', '👏', '🤝',
  '✊', '👊', '🤛', '🤜',
  '👆', '👇', '👈', '👉',  // 上下左右
  // 👇 脸
  '😊', '😂', '🤣', '😍', '🥰', '😎', '🤩', '🥺', '😭', '😤',
  '🤡',  // 小丑
  // 👇 心
  '❤️', '🧡', '💙', '💜', '💖',
  // 👇 其他
  '🎉', '🔥', '💪', '⭐', '✅', '💯', '🎊',
]

function getGroupedReactions(msg: any) {
  if (!msg.reactions?.length) return []
  const groups: Record<string, { type: string; count: number; self: boolean }> = {}
  msg.reactions.forEach((r: any) => {
    if (!groups[r.reactionType]) groups[r.reactionType] = { type: r.reactionType, count: 0, self: false }
    groups[r.reactionType].count++
    if (r.userId === myId) groups[r.reactionType].self = true
  })
  return Object.values(groups)
}

async function toggleReaction(msg: any, reactionType: string) {
  const hasSelf = msg.reactions?.some((r: any) => r.userId === myId && r.reactionType === reactionType)
  try {
    const updatedMsg: any = hasSelf
      ? await removeReaction(msg.id, reactionType)
      : await addReaction(msg.id, reactionType)
    // 直接用 API 返回的更新数据刷新 reactions
    if (updatedMsg && updatedMsg.reactions) {
      const idx = messages.value.findIndex(m => m.id === msg.id)
      if (idx !== -1) {
        messages.value[idx] = { ...messages.value[idx], reactions: updatedMsg.reactions }
      }
    }
  } catch {
    uni.showToast({ title: '操作失败', icon: 'none' })
  }
}

async function selectReaction(emoji: string) {
  const msg = pendingReactionMsg.value
  if (!msg) return
  showReactionPicker.value = false
  pendingReactionMsg.value = null
  await toggleReaction(msg, emoji)
}

/* ---- 长按模拟（touch/mouse 双兼容，用 flag 防重复触发） ---- */
let pressTimer: number | null = null
let pressFiredByTouch = false

function clearPressTimer() {
  if (pressTimer !== null) {
    clearTimeout(pressTimer)
    pressTimer = null
  }
}

function onLongPressStart(msg: any, $event?: any) {
  if (msg.isRecalled || isSystemMsg(msg)) return
  // touch 事件触发后，屏蔽后续 mouse 事件（H5 上 touch+鼠标会同时触发）
  if ($event?.type === 'touchstart') pressFiredByTouch = true
  if ($event?.type === 'mousedown' && pressFiredByTouch) return
  clearPressTimer()
  pressTimer = setTimeout(() => {
    pressTimer = null
    showMsgActions(msg)
  }, 600)
}

function onLongPressEnd($event?: any) {
  if ($event?.type === 'mouseup' || $event?.type === 'mouseleave') {
    if (pressFiredByTouch) { pressFiredByTouch = false; return }
  }
  if ($event?.type === 'touchend' || $event?.type === 'touchcancel') {
    // touch 结束后延迟重置 flag，防止 mouse 事件残留
    setTimeout(() => { pressFiredByTouch = false }, 100)
  }
  clearPressTimer()
}

async function recallMessage(msg: any) {
  try {
    await patch(`/im/messages/${msg.id}/recall`)
    // 替换整个对象触发响应式更新（兼容小程序 defineProperty 模式）
    const idx = messages.value.findIndex(m => m.id === msg.id)
    if (idx !== -1) {
      messages.value[idx] = { ...messages.value[idx], isRecalled: true }
    }
  } catch {
    uni.showToast({ title: '撤回失败', icon: 'none' })
  }
}

/** 输入框键盘事件：Enter 发送，Shift+Enter 换行（H5/App 生效，小程序走发送按钮） */
function handleInputKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter' && !e.shiftKey && !e.ctrlKey && !e.metaKey) {
    e.preventDefault()
    handleSend()
  }
}

/** 常用 emoji 列表（按类型分组排列） */
const emojiList = [
  // ===== 笑脸 =====
  '😊', '😂', '🤣', '😄', '😃', '😀', '😁', '😆',
  '😍', '🥰', '😘', '😗', '😙', '😚', '🤩', '😎',
  '🤗', '🫣', '😏', '😌', '😉', '🙃', '🥲', '😅',
  '🥺', '😢', '😭', '😤', '😡', '🤬', '😳', '🥴',
  '😱', '🤯', '😨', '😰', '🥶', '🥵', '🤒', '🤕',
  '🤔', '🫤', '😶', '😑', '😴', '🥳', '🤩', '🤡',
  '😈', '👻', '💀', '☠️', '👽', '🤖', '🎃', '👾',
  '🫠', '🫡', '🫢', '🫨', '🥹', '🥸', '🤪', '🤭',
  '🤫', '🤨', '🤓', '😐', '😒', '😔', '😞', '😟', '😕',
  '🙁', '😣', '😖', '😫', '😩', '😵', '🤤', '🤢',
  '🤮', '🤧',
  // ===== 手势 =====
  '👍', '👎', '👌', '✌️', '🤞', '🤟', '🤘', '🤙',
  '👋', '🤚', '✋', '🖐️', '👐', '🙌', '👏', '🤝',
  '✊', '👊', '🤛', '🤜', '👆', '👇', '👈', '👉',
  '☝️', '🖕', '🤌', '🫰', '🫵', '🤏', '🫸', '🫷',
  '🙏', '💅', '✍️', '🖖', '🤲', '🫱', '🫲', '🫳',
  '🫴', '💪', '🦾', '🦿', '🦵', '🦶', '👣', '👀',
  // ===== 爱心 =====
  '❤️', '🧡', '💛', '💚', '💙', '💜', '🖤', '🤍',
  '💕', '💔', '💖', '💗', '💘', '💝', '❣️', '💞',
  '🤎', '🩵', '🩷', '🩶', '♥️', '💌', '💋', '💟',
  // ===== 动物 =====
  '🐶', '🐱', '🐼', '🦊', '🐸', '🐷', '🐮', '🦁',
  '🐰', '🐵', '🐔', '🐧', '🐦', '🦄', '🐴', '🐝',
  '🦋', '🐛', '🐌', '🐞', '🐜', '🦀', '🐟', '🐠',
  '🐳', '🐬', '🦭', '🐊', '🦎', '🐍', '🐢', '🐙',
  '🐭', '🐹', '🐻', '🐨', '🐯', '🦓', '🐘', '🦒',
  '🦛', '🦏', '🐣', '🐤', '🐥', '🦆', '🦅', '🦉',
  '🦇', '🐺', '🐗', '🦈', '🐋', '🦂', '🦞', '🦑',
  // ===== 自然 & 食物 =====
  '🌸', '🌺', '🌻', '🌹', '🌷', '🌼', '🌿', '🍀',
  '🌴', '🌲', '🌵', '🌞', '🌙', '⭐', '☀️', '🌈',
  '☁️', '❄️', '🔥', '💧', '🌱', '☘️', '🍄', '🌾',
  '🍎', '🍊', '🍋', '🍌', '🍉', '🍇', '🍓', '🍑',
  '🍒', '🍍', '🥝', '🥑', '🥥', '🍆', '🥕', '🥦',
  '🌽', '🥜', '🌰', '🍔', '🍕', '🌮', '🌭', '🍟',
  '🥤', '🍺', '🍦', '🍰', '🍗', '🍖', '🥓', '🧀',
  '🥚', '🍳', '☕', '🧃', '🍵', '🧁', '🍩', '🍪',
  // ===== 物品 & 符号 =====
  '🎂', '🎁', '🎈', '🎉', '🎊', '🎯', '🎸', '🎵',
  '🎶', '🎤', '💰', '📱', '💻', '⌨️', '🔒', '🔑',
  '🔔', '📚', '✏️', '🖊️', '📎', '🏠', '🚗', '✈️',
  '🚀', '🚲', '🏃', '💃', '🤳', '🛴', '🏆', '🎮',
  '⚽', '🏀', '🥇', '🎲', '👑', '💎', '🔮', '🎪',
  '✅', '❌', '❓', '❗', '🆒', '🆕', '🆙', '🆗',
  '🔝', '💯', '🈵', '🈶', '🉑', '🆖', '🆑', '🆓',
  '📦', '📬', '📭', '📮', '🖨️', '🖥️', '💿', '📀',
  '🎬', '🎭', '🎨', '🎟️', '🏅', '🥈', '🥉', '🛒',
  '🔊', '📢', '📣', '📌', '📍', '🧩', '🧸', '🪀',
]

const showEmoji = ref(false)

function toggleEmojiPicker() {
  showEmoji.value = !showEmoji.value
}

function insertEmoji(emoji: string) {
  inputText.value += emoji
  // 插入后保持面板打开，可以连续选多个
}

/** 附件菜单 */
function showAttachMenu() {
  uni.showActionSheet({
    itemList: ['拍照', '从相册选择', '文件'],
    success: (res) => {
      if (res.tapIndex === 0) pickAndSend('camera')
      else if (res.tapIndex === 1) pickAndSend('album')
      else if (res.tapIndex === 2) pickAndSend('file')
    },
  })
}

/** 选择附件并发送 */
function pickAndSend(source: 'camera' | 'album' | 'file') {
  if (source === 'file') {
    // #ifdef APP-PLUS
    uni.chooseFile({ count: 1, type: 'all',
      success: (res) => uploadAndSend(res.tempFilePaths[0]),
      fail: () => {},
    })
    // #endif
    // #ifndef APP-PLUS
    uni.showToast({ title: '文件仅支持 App 端', icon: 'none' })
    // #endif
    return
  }
  uni.chooseImage({ count: 1, sourceType: [source],
    success: (res) => uploadAndSend(res.tempFilePaths[0]),
    fail: () => {},
  })
}

/** 上传文件 → 拿到 fileId → 发消息 */
async function uploadAndSend(filePath: string) {
  uni.showLoading({ title: '上传中...' })
  try {
    const uploaded: any = await uploadFile(filePath)
    uni.hideLoading()
    if (!uploaded?.id) { uni.showToast({ title: '上传返回异常', icon: 'none' }); return }
    await sendMessage({
      conversationId: conversationId.value,
      content: uploaded.fileName || '附件',
      messageType: 'File',
      fileId: uploaded.id,
    })
    // SignalR 会推送消息，无需手动添加
  } catch (e: any) {
    uni.hideLoading()
    console.warn('[upload] 上传失败', e)
    uni.showToast({ title: e?.message || '上传失败', icon: 'none' })
  }
}

/** 判断文件名是否为图片 */
function isImageFile(fileName: string): boolean {
  return /\.(jpg|jpeg|png|gif|webp|bmp)$/i.test(fileName || '')
}

/** 构造文件下载 URL */
function getFileUrl(fileId: string): string {
  const token = uni.getStorageSync('token') || ''
  return `${BASE_URL}/files/${fileId}/download?token=${encodeURIComponent(token)}`
}

/** 预览图片（优先用已缓存的临时路径） */
function previewFile(fileId: string) {
  const url = imageCache.value[fileId] || getFileUrl(fileId)
  if (imageCache.value[fileId]) {
    uni.previewImage({ urls: [url] })
  } else {
    // 还没缓存，先下载再预览
    loadImage(fileId).then((tempPath) => {
      uni.previewImage({ urls: [tempPath || getFileUrl(fileId)] })
    })
  }
}

/** 下载非图片文件 */
function downloadFile(fileId: string, fileName: string) {
  const token = uni.getStorageSync('token') || ''
  uni.showLoading({ title: '下载中...' })
  uni.downloadFile({
    url: `${BASE_URL}/files/${fileId}/download`,
    header: { Authorization: `Bearer ${token}` },
    success: (res) => {
      uni.hideLoading()
      if (res.statusCode !== 200) { uni.showToast({ title: '下载失败', icon: 'none' }); return }
      uni.saveFile({ tempFilePath: res.tempFilePath,
        success: () => uni.showToast({ title: '下载完成', icon: 'success' }),
        fail: () => {
          uni.openDocument({ filePath: res.tempFilePath, showMenu: true,
            fail: () => uni.showToast({ title: '无法打开此文件', icon: 'none' }),
          })
        },
      })
    },
    fail: () => { uni.hideLoading(); uni.showToast({ title: '下载失败', icon: 'none' }) },
  })
}

/** 判断是否为系统消息 */
function isSystemMsg(msg: any): boolean {
  return msg.messageType === 'System' || msg.content?.startsWith?.('__SYSTEM_GROUP_JOIN__')
}

/** 转义正则特殊字符 */
function escapeRegex(str: string): string {
  return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

/* ---- 草稿保存（退出聊天页时保存输入内容，重新进入时恢复） ---- */
function getDraftKey(): string {
  return `draft_${conversationId.value}_${myId}`
}

function saveDraft() {
  if (!conversationId.value) return
  const text = inputText.value.trim()
  if (!text && mentionMembers.value.length === 0) return // 无内容不保存
  uni.setStorageSync(getDraftKey(), JSON.stringify({
    text: inputText.value,
    mentions: mentionMembers.value,
  }))
}

function loadDraft() {
  if (!conversationId.value) return
  try {
    const raw = uni.getStorageSync(getDraftKey())
    if (raw) {
      const draft = JSON.parse(raw)
      if (draft.text) inputText.value = draft.text
      if (draft.mentions?.length) mentionMembers.value = draft.mentions
    }
  } catch { /* 解析失败忽略 */ }
}

function clearDraft() {
  if (!conversationId.value) return
  uni.removeStorageSync(getDraftKey())
}

/** 解析 @提及 分段，返回普通文本和 @名字 交替的片段数组 */
function parseMentions(msg: any): Array<{ text: string; isMention: boolean }> {
  const text = msg.content || ''
  if (!text) return [{ text: '', isMention: false }]

  // 从 mentionUserIds + 群成员 构建精确高亮的名字列表（支持空格：@USER A）
  const names: string[] = []
  if (msg.mentionUserIds?.length && groupMembers.value.length) {
    for (const uid of msg.mentionUserIds) {
      const member = groupMembers.value.find((m: any) => m.userId === uid)
      const name = member?.realName || member?.username
      if (name) names.push(name)
    }
  }
  // 长名字优先，避免"张三"匹配到"张三丰"
  names.sort((a, b) => b.length - a.length)

  // 构建正则：有精确名字则用精确匹配，否则退回到通用 @\w+ 匹配
  let regex: RegExp
  if (names.length > 0) {
    const escaped = names.map(n => escapeRegex(n))
    regex = new RegExp(`@(?:${escaped.join('|')})(?=[\\s，。、！？；：,\.!?;:\\)>\\]}]|$)`, 'g')
  } else {
    regex = /@[一-龥\w\-]+/g
  }

  const parts: Array<{ text: string; isMention: boolean }> = []
  let lastIndex = 0
  let match: RegExpExecArray | null
  while ((match = regex.exec(text)) !== null) {
    if (match.index > lastIndex) {
      parts.push({ text: text.slice(lastIndex, match.index), isMention: false })
    }
    parts.push({ text: match[0], isMention: true })
    lastIndex = match.index + match[0].length
  }
  if (lastIndex < text.length) {
    parts.push({ text: text.slice(lastIndex), isMention: false })
  }
  return parts.length ? parts : [{ text, isMention: false }]
}

/** 格式化系统消息文字 */
function formatSystemMsg(msg: any): string {
  const content = msg.content || ''
  if (content.startsWith('__SYSTEM_GROUP_JOIN__')) {
    const parts = content.split(':')
    const memberNames = parts[1] || ''
    const groupName = parts[2] || ''
    if (msg.senderId === myId) {
      return `您已将${memberNames.replace(/,/g, '、')}拉入群聊${groupName}`
    } else {
      return `您已被${msg.senderName || '对方'}拉入群聊${groupName}`
    }
  }
  return content
}

function formatTime(t: string) {
  if (!t) return ''
  const d = new Date(t)
  const now = new Date()
  const hh = String(d.getHours()).padStart(2, '0')
  const mm = String(d.getMinutes()).padStart(2, '0')
  if (d.toDateString() === now.toDateString()) {
    return `${hh}:${mm}`
  }
  return `${d.getMonth() + 1}/${String(d.getDate()).padStart(2, '0')} ${hh}:${mm}`
}

/** 获取消息日期的分组标签 */
const WEEKDAYS = ['日', '一', '二', '三', '四', '五', '六']
function getDateLabel(t: string): string {
  if (!t) return ''
  const d = new Date(t)
  const now = new Date()
  const today = now.toDateString()
  const yesterday = new Date(now)
  yesterday.setDate(now.getDate() - 1)
  const yStr = yesterday.toDateString()

  if (d.toDateString() === today) return '今天'
  if (d.toDateString() === yStr) return '昨天'

  // 本周内显示"本周x"
  const nowDay = now.getDay()
  const monday = new Date(now)
  monday.setDate(now.getDate() - ((nowDay + 6) % 7))
  if (d >= monday && d < new Date(monday.getTime() + 7 * 86400000)) {
    return `本周${WEEKDAYS[d.getDay()]}`
  }

  // 今年内显示 M月D日
  if (d.getFullYear() === now.getFullYear()) {
    return `${d.getMonth() + 1}月${d.getDate()}日`
  }
  // 跨年显示 YYYY年M月D日
  return `${d.getFullYear()}年${d.getMonth() + 1}月${d.getDate()}日`
}

/** 时间分组展示列表：在日期变化处插入分隔栏 */
const displayItems = computed(() => {
  const items: Array<{ type: 'separator'; label: string } | { type: 'message'; data: any }> = []
  let lastDate = ''
  for (const msg of messages.value) {
    const dateKey = msg.createdAt ? new Date(msg.createdAt).toDateString() : ''
    if (dateKey && dateKey !== lastDate) {
      items.push({ type: 'separator', label: getDateLabel(msg.createdAt) })
      lastDate = dateKey
    }
    items.push({ type: 'message', data: msg })
  }
  return items
})

const colors = ['#409EFF', '#67C23A', '#E6A23C', '#F56C6C', '#909399', '#B37FEB', '#00BFA5', '#FF7043']
function getAvatarColor(id: string): string {
  let hash = 0
  for (let i = 0; i < id.length; i++) hash = ((hash << 5) - hash) + id.charCodeAt(i)
  return colors[Math.abs(hash) % colors.length]
}

/** 标记已读 */
function markAsRead() {
  patch(`/im/conversations/${conversationId.value}/read`).catch(() => {})
}

/** SignalR 消息回调 */
let signalRHandler: ((msg: any) => void) | null = null

/** 跳转群管理页 */
function goGroupManage() {
  uni.navigateTo({ url: `/pages/im/group-manage?conversationId=${conversationId.value}` })
}

onLoad((options) => {
  conversationId.value = options?.conversationId || ''
  convType.value = options?.type || ''
  convName.value = options?.name ? decodeURIComponent(options.name) : '聊天'

  if (convName.value) {
    uni.setNavigationBarTitle({ title: convName.value })
  }

  if (conversationId.value) {
    loadMessages(true)
    markAsRead()
    loadDraft() // 恢复草稿
  }

  // 群聊加载公告 + 群成员（用于 @提及）
  if (convType.value === 'Group' && conversationId.value) {
    loadGroupMembers()
    getAnnouncement(conversationId.value).then((res: any) => {
      if (res?.announcement) {
        announcement.value = res.announcement
        // 未读过的公告自动弹窗
        const seenKey = `announce_seen_${conversationId.value}_${myId}`
        const lastSeen = uni.getStorageSync(seenKey) || ''
        if (res.announcementUpdatedAt && res.announcementUpdatedAt !== lastSeen) {
          showAnnouncePopup.value = true
          uni.setStorageSync(seenKey, res.announcementUpdatedAt)
        }
      }
    }).catch(() => {})
  }

  // 监听 SignalR 实时消息
  signalRHandler = (raw: any) => {
    // ReceiveMessage 处理
    if (raw.type === 1 && raw.target === 'ReceiveMessage') {
      const msg = raw.arguments?.[0]
      if (!msg || msg.conversationId !== conversationId.value) return
      // 避免重复添加（自己发的消息已通过乐观更新 + Object.assign 有了真实 ID）
      const exists = messages.value.some(m => m.id === msg.id)
      if (exists) return
      // 处理竞态：SignalR 推送先于 HTTP 响应到达时，移除临时消息
      if (msg.senderId === myId) {
        messages.value = messages.value.filter(m => !(m.id.startsWith('temp-') && m.content === msg.content))
      }
      messages.value.push(msg)
      // 图片消息下载到本地临时路径才能显示
      if (msg.fileId && isImageFile(msg.fileName)) loadImage(msg.fileId)
      scrollToBottom()
      return
    }

    // MessageReactionUpdated 处理
    if (raw.type === 1 && raw.target === 'MessageReactionUpdated') {
      const updatedMsg = raw.arguments?.[0]
      if (!updatedMsg?.id) return
      const idx = messages.value.findIndex(m => m.id === updatedMsg.id)
      if (idx !== -1) {
        messages.value[idx] = { ...messages.value[idx], reactions: updatedMsg.reactions || [] }
      }
      return
    }

    // MessageRecalled 处理
    if (raw.type === 1 && raw.target === 'MessageRecalled') {
      const data = raw.arguments?.[0]
      if (!data?.id) return
      const idx = messages.value.findIndex(m => m.id === data.id)
      if (idx !== -1) {
        messages.value[idx] = { ...messages.value[idx], isRecalled: true }
      }
    }
  }
  signalR.onMessage(signalRHandler)
})

// 离开页面时清理 SignalR 监听 + 保存草稿
onUnload(() => {
  saveDraft()
  signalR.offMessage(signalRHandler)
  signalRHandler = null
})

// 页面隐藏时也保存草稿（切后台、切 tab 等）
onHide(() => {
  saveDraft()
})
</script>

<style scoped>
.chat-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
  background: linear-gradient(180deg, #f4f8ff 0%, #f6f8fc 100%);
}

/* 群信息头 */
.group-info-bar {
  display: flex;
  align-items: center;
  padding: 18rpx 28rpx;
  background: #fff;
  border-bottom: 1rpx solid #edf1f7;
  flex-shrink: 0;
}
.group-info-bar:active {
  background: #f8fbff;
}
.group-info-avatar {
  width: 64rpx;
  height: 64rpx;
  border-radius: 18rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 18rpx;
  flex-shrink: 0;
}
.group-info-avatar-text {
  color: #fff;
  font-size: 28rpx;
  font-weight: 600;
}
.group-info-text {
  flex: 1;
  min-width: 0;
}
.group-info-name {
  font-size: 28rpx;
  font-weight: 700;
  color: #111827;
  display: block;
}
.group-info-hint {
  font-size: 22rpx;
  color: #a8b0c2;
  margin-top: 2rpx;
  display: block;
}

/* 消息列表 */
.msg-list {
  flex: 1;
  height: 0; /* 强制 flex-basis:0，防止 flex 计算溢出 */
  padding-top: 24rpx;
}
.msg-item {
  display: flex;
  margin-bottom: 30rpx;
  padding: 0 28rpx;
}
.date-sep-bar {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20rpx 28rpx 10rpx;
}
.date-sep-text {
  font-size: 22rpx;
  color: #b0b8c4;
  background: #f0f2f5;
  padding: 4rpx 18rpx;
  border-radius: 20rpx;
}
.msg-self {
  flex-direction: row-reverse;
}
.msg-avatar {
  width: 68rpx;
  height: 68rpx;
  border-radius: 20rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.msg-avatar-text {
  color: #fff;
  font-size: 26rpx;
  font-weight: 600;
}
.msg-body {
  margin: 0 16rpx;
  max-width: 72%;
}
.msg-sender {
  font-size: 22rpx;
  color: #7b8494;
  margin-bottom: 6rpx;
  text-align: left;
}
.msg-self .msg-sender {
  text-align: right;
}
.msg-bubble {
  display: inline-block;
  max-width: 100%;
  text-align: left;
  background: #fff;
  padding: 18rpx 22rpx;
  border-radius: 22rpx 22rpx 22rpx 6rpx;
  box-shadow: 0 10rpx 28rpx rgba(31, 49, 84, 0.08);
}
.msg-bubble-self {
  background: linear-gradient(135deg, #1f6fff 0%, #18b7ff 100%);
  border-radius: 22rpx 22rpx 6rpx 22rpx;
  box-shadow: 0 12rpx 30rpx rgba(31, 111, 255, 0.22);
}
/* 自己在右边的消息整体右对齐 */
.msg-self .msg-body {
  text-align: right;
}
/* 撤回消息 → 系统提示小字（居中，无气泡无头像） */
.recalled-line {
  text-align: center;
  padding: 16rpx 0;
  width: 100%;
}
.recalled-text {
  font-size: 22rpx;
  color: #a8b0c2;
  background: rgba(255, 255, 255, 0.76);
  padding: 8rpx 18rpx;
  border-radius: 999rpx;
}
.msg-text {
  font-size: 28rpx;
  color: #111827;
  line-height: 1.6;
  word-break: break-all;
  white-space: pre-wrap;
}
.msg-mention {
  color: #1f6fff;
}
.msg-bubble-self .msg-text {
  color: #fff;
}
.msg-bubble-self .msg-mention {
  color: #b3e0ff;
}

/* 图片消息 */
.img-bubble {
  background: transparent !important;
  box-shadow: none !important;
  padding: 4rpx !important;
  border-radius: 16rpx !important;
}
.msg-image {
  width: 320rpx;
  height: auto;
  max-height: 480rpx;
  border-radius: 12rpx;
  display: block;
}
/* 文件消息 */
.file-name {
  font-size: 26rpx;
  color: #1f6fff;
  display: block;
  margin-bottom: 6rpx;
}
.file-download {
  font-size: 22rpx;
  color: #fff;
  background: #1f6fff;
  padding: 6rpx 16rpx;
  border-radius: 999rpx;
  display: inline-block;
}
.msg-bubble-self .file-name {
  color: #e0f0ff;
}
.msg-bubble-self .file-download {
  background: rgba(255,255,255,0.25);
}

.msg-time {
  font-size: 20rpx;
  color: #a8b0c2;
  white-space: nowrap;
}
/* 底部输入区（始终固定在底部） */
.input-area {
  flex-shrink: 0;
  position: sticky;
  bottom: 0;
  z-index: 10;
  background: rgba(255, 255, 255, 0.96);
  border-top: 1rpx solid #edf1f7;
  box-shadow: 0 -12rpx 30rpx rgba(31, 49, 84, 0.06);
}
.input-bar {
  display: flex;
  align-items: center;
  padding: 16rpx 20rpx calc(16rpx + env(safe-area-inset-bottom));
}
.input-emoji,
.input-attach {
  font-size: 38rpx;
  padding: 8rpx 10rpx;
}
.input-field {
  flex: 1;
  min-height: 72rpx;
  max-height: 200rpx;
  background: #f6f8fc;
  border-radius: 36rpx;
  padding: 16rpx 24rpx;
  font-size: 28rpx;
  border: 1rpx solid #edf1f7;
  resize: none;
}
.placeholder {
  color: #a8b0c2;
  font-size: 28rpx;
}
.send-btn {
  margin-left: 12rpx;
  height: 72rpx;
  line-height: 72rpx;
  padding: 0 28rpx;
  background: #1f6fff;
  color: #fff;
  font-size: 28rpx;
  border-radius: 36rpx;
  border: none;
  flex-shrink: 0;
  font-weight: 600;
}
.send-btn[disabled] {
  opacity: 0.4;
}

/* Emoji 选择面板 */
.emoji-panel {
  background: #fff;
  border-top: 1rpx solid #edf1f7;
  padding: 12rpx 16rpx calc(12rpx + env(safe-area-inset-bottom));
  max-height: 420rpx;
  overflow-y: auto;
  flex-shrink: 0;
}
.emoji-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 4rpx;
}
.emoji-item {
  font-size: 44rpx;
  width: 72rpx;
  height: 72rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 12rpx;
}
.emoji-item:active {
  background: #f0f2f5;
  transform: scale(1.15);
}

/* ===== @提及按钮 ===== */
.input-at {
  font-size: 36rpx;
  font-weight: 700;
  color: #1f6fff;
  padding: 8rpx 12rpx;
}
.input-at:active { opacity: 0.6; }

/* ===== @提及chips ===== */
.mention-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 6rpx;
  padding: 8rpx 20rpx 0;
}
.mention-chip {
  display: inline-flex;
  align-items: center;
  gap: 4rpx;
  font-size: 22rpx;
  color: #1f6fff;
  background: #eef4ff;
  padding: 4rpx 12rpx;
  border-radius: 999rpx;
}
.mention-chip-remove {
  font-size: 18rpx;
  color: #7b8494;
  margin-left: 2rpx;
}

/* ===== @提及选择器 ===== */
.mention-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0,0,0,0.45);
  z-index: 999;
  display: flex;
  align-items: flex-end;
  justify-content: center;
}
.mention-picker {
  width: 100%;
  max-width: 600rpx;
  max-height: 60vh;
  background: #fff;
  border-radius: 28rpx 28rpx 0 0;
  padding: 28rpx 24rpx calc(28rpx + env(safe-area-inset-bottom));
  display: flex;
  flex-direction: column;
}
.mention-picker-title {
  font-size: 30rpx;
  font-weight: 700;
  text-align: center;
  color: #111827;
  margin-bottom: 16rpx;
}
.mention-search-input {
  height: 64rpx;
  background: #f6f8fc;
  border-radius: 16rpx;
  padding: 0 20rpx;
  font-size: 26rpx;
  border: 1rpx solid #edf1f7;
  margin-bottom: 16rpx;
}
.mention-scroll {
  max-height: 350rpx;
  margin-bottom: 16rpx;
}
.mention-row {
  display: flex;
  align-items: center;
  padding: 16rpx 12rpx;
  border-radius: 12rpx;
}
.mention-row:active { background: #f0f2f5; }
.mention-row.selected { background: #eef4ff; }
.mention-avatar {
  width: 48rpx; height: 48rpx;
  border-radius: 12rpx;
  display: flex; align-items: center; justify-content: center;
  margin-right: 16rpx; flex-shrink: 0;
}
.mention-avatar-text { color: #fff; font-size: 22rpx; }
.mention-name { flex: 1; font-size: 26rpx; color: #111827; }
.mention-check {
  width: 32rpx; height: 32rpx;
  border: 2rpx solid #cfd6e3;
  border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
}
.mention-check.checked { background: #1f6fff; border-color: #1f6fff; }
.mention-check-mark { color: #fff; font-size: 18rpx; font-weight: bold; }
.mention-empty { text-align: center; padding: 40rpx; font-size: 24rpx; color: #a8b0c2; }
.mention-btns { display: flex; gap: 16rpx; }
.mention-cancel, .mention-confirm {
  flex: 1; height: 72rpx; line-height: 72rpx;
  font-size: 26rpx; border-radius: 36rpx; border: none; text-align: center;
}
.mention-cancel { background: #f6f8fc; color: #374151; }
.mention-confirm { background: #1f6fff; color: #fff; }
.mention-confirm[disabled] { opacity: 0.4; }

/* ===== 时间 + 表情回复（同一行） ===== */
.msg-meta {
  display: flex;
  align-items: center;
  gap: 8rpx;
  margin-top: 4rpx;
  flex-wrap: wrap;
}
.msg-self .msg-meta {
  justify-content: flex-end;
}
.msg-reactions {
  display: inline-flex;
  flex-wrap: wrap;
  gap: 3rpx;
}
.reaction-item {
  display: inline-flex;
  align-items: center;
  gap: 2rpx;
  padding: 0 8rpx;
  border-radius: 999rpx;
  border: 1rpx solid #e8eaed;
  background: #fff;
  font-size: 20rpx;
  line-height: 32rpx;
  height: 32rpx;
}
.reaction-item:active { transform: scale(1.15); }
.reaction-self { background: #eef4ff; border-color: #1f6fff; }
.reaction-count { font-size: 18rpx; color: #7b8494; margin-left: 1rpx; }
.reaction-self .reaction-count { color: #1f6fff; }

/* 表情回复选择器 */
.reaction-picker-overlay {
  position: fixed;
  top: 0; bottom: 0; left: 0; right: 0;
  z-index: 20;
  background: rgba(0,0,0,0.3);
  display: flex;
  align-items: flex-end;
  justify-content: center;
}
.reaction-picker {
  background: #fff;
  border-radius: 28rpx 28rpx 0 0;
  padding: 28rpx 24rpx calc(28rpx + env(safe-area-inset-bottom));
  width: 100%;
  max-height: 65vh;
  overflow-y: auto;
}
.reaction-picker-title {
  font-size: 28rpx;
  font-weight: 600;
  color: #111827;
  text-align: center;
  display: block;
  margin-bottom: 20rpx;
}
.reaction-picker-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 12rpx;
  justify-content: center;
}
.reaction-picker-emoji {
  font-size: 56rpx;
  width: 88rpx;
  height: 88rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 20rpx;
  background: #f6f8fc;
}
.reaction-picker-emoji:active {
  background: #eef4ff;
  transform: scale(1.2);
}

/* ===== 群公告 ===== */
.announce-banner {
  display: flex;
  align-items: center;
  padding: 16rpx 28rpx;
  background: #fffbe6;
  border-bottom: 1rpx solid #ffe58f;
  flex-shrink: 0;
  gap: 12rpx;
}
.announce-banner:active { background: #fff7d6; }
.announce-banner-icon { font-size: 28rpx; flex-shrink: 0; }
.announce-banner-text {
  flex: 1;
  font-size: 24rpx;
  color: #8c6e00;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.announce-banner-close {
  font-size: 28rpx;
  color: #997a00;
  padding: 4rpx 8rpx;
  flex-shrink: 0;
}
.announce-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0,0,0,0.45);
  z-index: 999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 60rpx;
}
.announce-popup {
  width: 100%;
  max-width: 600rpx;
  max-height: 70vh;
  background: #fff;
  border-radius: 28rpx;
  padding: 36rpx;
  display: flex;
  flex-direction: column;
}
.announce-popup-title {
  font-size: 32rpx;
  font-weight: 700;
  text-align: center;
  margin-bottom: 20rpx;
  color: #111827;
}
.announce-popup-content {
  flex: 1;
  font-size: 28rpx;
  color: #374151;
  line-height: 1.7;
  max-height: 50vh;
  margin-bottom: 20rpx;
}
.announce-popup-close {
  height: 76rpx;
  line-height: 76rpx;
  background: #1f6fff;
  color: #fff;
  font-size: 28rpx;
  border-radius: 20rpx;
  border: none;
  text-align: center;
}
</style>
