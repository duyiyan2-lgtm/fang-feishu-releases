import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import {
  listConversations, listMessages, sendMessageHttp, recallMessageHttp, markConversationRead,
  createConversation, adaptConversation, adaptMessage, getConversationReadReceipts
} from '@/api/im'
import { ElMessage } from '@/api/toast'
import hub from '@/utils/signalr'
import { useUserStore } from '@/stores/user'

const MAX_CACHED_MESSAGES = 300

export const useMessagesStore = defineStore('messages', () => {
  const userStore = useUserStore()
  const conversations = ref([]) // adaptConversation 后的列表
  const messagesMap = ref({})   // { [conversationId]: Message[] }
  const activeId = ref(null)
  const loading = ref(false)
  const connected = ref(false)
  const online = ref(true) // 兼容旧字段
  let reconnectTimer = null

  const activeConv = computed(() => conversations.value.find((c) => c.id === activeId.value))
  const activeMessages = computed(() => messagesMap.value[activeId.value] || [])

  function myId() { return userStore.userInfo?.id }

  /** 拉会话列表（合并而非覆盖：保留 localStorage 中的未读数 / 自定义字段） */
  async function fetchConversations() {
    loading.value = true
    try {
      const list = await listConversations()
      const fresh = (list || []).map(c => adaptConversation(c, myId()))
      // 合并：保留旧会话的未读数、已存在消息
      const map = new Map(conversations.value.map(c => [c.id, c]))
      const merged = fresh.map(n => {
        const old = map.get(n.id)
        if (old) return { ...n, unread: Number.isFinite(n.unread) ? n.unread : (old.unread || 0),
          lastMessage: n.lastMessage || old.lastMessage,
          lastSender: n.lastSender || old.lastSender,
          lastTime: n.lastTime || old.lastTime
        }
        return n
      })
      conversations.value = merged
    } catch (e) {
      console.error('[messages] fetchConversations', e)
    } finally {
      loading.value = false
    }
  }

  /**
   * 从本地状态中移除某个会话（用于群解散 / 退群时）
   */
  function removeLocalConversation(id) {
    conversations.value = conversations.value.filter(c => c.id !== id)
    delete messagesMap.value[id]
    if (activeId.value === id) activeId.value = null
  }

  /** 选择会话 */
  async function selectConversation(id) {
    activeId.value = id
    const hasCache = Array.isArray(messagesMap.value[id]) && messagesMap.value[id].length > 0
    // 有缓存时先即时展示，再后台补齐断线期间漏掉的消息；无缓存时等待首屏历史。
    if (!hasCache) {
      await loadMessages(id)
    } else {
      void loadMessages(id)
    }
    // 即使 Hub 尚未建连，也会排队；连接成功或重连后自动补订阅。
    hub.joinConversation?.(id)
    // 标记已读
    try { await markConversationRead(id) } catch {}
    // 本地未读清零
    const c = conversations.value.find(x => x.id === id)
    if (c) c.unread = 0
  }

  /** 拉历史消息 */
  async function loadMessages(conversationId) {
    try {
      const res = await listMessages(conversationId, 1, 50)
      const items = res?.items || res || []
      // 后端倒序返回（最新在前），UI 显示需正序
      if (!messagesMap.value[conversationId]) messagesMap.value[conversationId] = []
      items.map(m => adaptMessage(m, myId())).reverse()
        .forEach(m => appendMessage(conversationId, m))
      pruneMessageCache(conversationId)
    } catch (e) {
      console.error('[messages] loadMessages', e)
      // 离线或后端短暂不可用时保留本地缓存，避免聊天记录突然清空。
    }
  }

  /** 发送消息 */
  async function sendMessage(content) {
    const text = (content || '').trim()
    if (!text || !activeId.value) return false
    const convId = activeId.value
    const optimistic = {
      id: `tmp-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`,
      conversationId: convId,
      content: text,
      sender: 'me',
      senderId: myId(),
      senderName: userStore.userInfo?.realName || userStore.userInfo?.name || '我',
      time: formatHM(new Date()),
      recalled: false,
      pending: true,
      failed: false
    }
    // 先显示本地消息，再等待网络往返，弱网下输入区也不会出现“按了没反应”。
    appendMessage(convId, optimistic)
    updateConvPreview(convId, text, optimistic.senderName, new Date().toISOString())
    try {
      // 优先 SignalR，失败再 HTTP 兜底
      try {
        await hub.sendMessage(convId, text, 'Text')
        // SignalR 的 ReceiveMessage 回调会把对应临时消息替换为服务端消息。
      } catch {
        const msg = await sendMessageHttp({ conversationId: convId, content: text, messageType: 'Text' })
        const m = adaptMessage(msg, myId())
        appendMessage(convId, m)
      }
      return true
    } catch (e) {
      const failedMessage = messagesMap.value[convId]?.find(item => item.id === optimistic.id)
      if (failedMessage) {
        failedMessage.pending = false
        failedMessage.failed = true
      }
      ElMessage({ message: '发送失败：' + (e.message || '未知错误'), type: 'error' })
      return false
    }
  }

  /** 撤回消息 */
  async function recallMessage(messageId) {
    try {
      await recallMessageHttp(messageId)
      // 本地立即标记
      for (const cid of Object.keys(messagesMap.value)) {
        const m = messagesMap.value[cid].find(x => x.id === messageId)
        if (m) m.recalled = true
      }
      ElMessage({ message: '已撤回消息', type: 'success' })
    } catch (e) {
      ElMessage({ message: '撤回失败', type: 'error' })
    }
  }

  /** SignalR: 收到新消息 */
  function onReceiveMessage(msg) {
    const m = adaptMessage(msg, myId())
    // 去重：避免多次 listener 触发导致重复消息
    const conv = messagesMap.value[m.conversationId] || []
    if (conv.some(x => x.id === m.id)) return
    appendMessage(m.conversationId, m)
    updateConvPreview(m.conversationId, m.content, m.senderName, m.raw?.createdAt)
    // 如果不是我发的且不是当前会话，未读 +1
    if (m.sender === 'other' && m.conversationId !== activeId.value) {
      const c = conversations.value.find(x => x.id === m.conversationId)
      if (c) c.unread = (c.unread || 0) + 1
      else void fetchConversations()
    } else if (m.sender === 'other' && m.conversationId === activeId.value) {
      const c = conversations.value.find(x => x.id === m.conversationId)
      if (c) c.unread = 0
      if (typeof document === 'undefined' || document.visibilityState === 'visible') {
        void markConversationRead(m.conversationId).catch(() => {})
      }
    }
    // 触发系统级通知 + 标题闪烁
    if (m.sender === 'other') {
      import('@/utils/notification').then(({ notifyNewMessage }) => {
        notifyNewMessage({
          id: m.id,
          senderName: m.senderName,
          content: m.content,
          conversationId: m.conversationId
        }, (clickedMsg) => {
          // 点击通知 → 切到对应会话
          selectConversation(clickedMsg.conversationId)
        }, {
          activeConversationId: activeId.value
        })
      })
    }
  }

  /** SignalR: 消息被撤回 */
  function onMessageRecalled(payload) {
    const { messageId } = payload || {}
    if (!messageId) return
    for (const cid of Object.keys(messagesMap.value)) {
      const m = messagesMap.value[cid].find(x => x.id === messageId)
      if (m) m.recalled = true
    }
  }

  function appendMessage(convId, m) {
    if (!messagesMap.value[convId]) messagesMap.value[convId] = []
    // 去重 1：按 id
    if (m.id && messagesMap.value[convId].some(x => x.id === m.id)) return

    // 服务端回传自己的消息时，优先替换对应的乐观消息。
    // 必须先于内容去重，否则真实消息会被当成重复消息丢弃。
    const idx = messagesMap.value[convId].findIndex(x =>
      x.pending && m.sender === 'me' && x.content === m.content &&
      (x.senderId === m.senderId || x.senderName === m.senderName)
    )
    if (idx > -1) {
      m._ts = Date.now()
      m.readers = m.readers || []
      m.readCount = m.readers.length || 0
      messagesMap.value[convId].splice(idx, 1, m)
      return
    }

    m._ts = Date.now()
    // 新增：已读回执字段
    m.readers = m.readers || []
    m.readCount = m.readers.length || 0

    // 否则 push
    messagesMap.value[convId].push(m)
    pruneMessageCache(convId)
  }

  /** 限制长时间在线时的 DOM、内存和 localStorage 占用。 */
  function pruneMessageCache(conversationId) {
    const ids = conversationId ? [conversationId] : Object.keys(messagesMap.value)
    for (const id of ids) {
      const items = messagesMap.value[id]
      if (Array.isArray(items) && items.length > MAX_CACHED_MESSAGES) {
        messagesMap.value[id] = items.slice(-MAX_CACHED_MESSAGES)
      }
    }
  }

  /** 只按服务端消息 ID 清理持久化历史；相同内容可能是用户真实重复发送，不能误删。 */
  function normalizeMessageCache() {
    for (const convId of Object.keys(messagesMap.value)) {
      const seen = new Set()
      messagesMap.value[convId] = (messagesMap.value[convId] || []).filter((message) => {
        if (!message?.id) return true
        if (seen.has(message.id)) return false
        seen.add(message.id)
        return true
      })
    }
  }

  /**
   * SignalR: 消息已读回执
   */
  function handleMessageRead(p) {
    if (!p?.messageId) return
    for (const cid of Object.keys(messagesMap.value)) {
      const arr = messagesMap.value[cid]
      const m = arr.find(x => x.id === p.messageId)
      if (m) {
        if (!m.readers) m.readers = []
        if (!m.readers.some(r => r.userId === p.readerId)) {
          m.readers.push({
            userId: p.readerId,
            realName: p.realName || '',
            readAt: p.readAt || new Date().toISOString()
          })
          m.readCount = m.readers.length
        }
        break
      }
    }
  }

  function updateConvPreview(convId, content, senderName, isoTime) {
    const c = conversations.value.find(x => x.id === convId)
    if (!c) return
    c.lastMessage = content
    c.lastSender = senderName
    c.lastTime = formatTime(isoTime)
    c.lastIsRecalled = false
  }

  /** 启动 SignalR 连接 */
  function initHub() {
    hub.handlers.onMessage = onReceiveMessage
    hub.handlers.onRecalled = onMessageRecalled
    hub.handlers.onMessageRead = handleMessageRead
    hub.handlers.onConversation = () => { void fetchConversations() }
    hub.handlers.onConnectionStateChanged = (status) => {
      connected.value = status === 'connected'
      online.value = status !== 'disconnected'
      if (status === 'disconnected') scheduleHubReconnect()
    }
    hub.handlers.onReconnected = async () => {
      await fetchConversations()
      if (activeId.value) await loadMessages(activeId.value)
    }
    // 好友请求实时通知（动态 import 避免循环依赖）
    import('./friend').then(({ useFriendStore }) => {
      const fs = useFriendStore()
      hub.handlers.onFriendRequestReceived = (p) => {
        if (p?.id && !fs.pendingReceived.some(item => item.id === p.id)) {
          const requester = { id: p.requesterId, realName: p.requesterName }
          fs.pendingReceived.push({
            id: p.id,
            user: requester,
            requester,
            createdAt: p.createdAt
          })
        }
      }
      hub.handlers.onFriendRequestAccepted = (p) => {
        ElMessage({ message: `${p?.friendName || '对方'} 接受了你的好友请求`, type: 'success' })
        fs.fetchAll()
      }
      hub.handlers.onFriendRequestRejected = () => {
        ElMessage({ message: '对方拒绝了你的好友请求', type: 'info' })
      }
      // 通知中心实时更新
      hub.handlers.onReceiveNotification = async (p) => {
        if (!p?.id) return
        try {
          // 过滤自己发消息触发的自己通知（避免 user_a 收到自己消息的提醒）
          const { useUserStore } = await import('./user')
          const meId = useUserStore().userInfo?.id
          if (meId && (p.senderId === meId || p.userId === meId || p.fromUserId === meId)) return
          const { useNotificationsStore } = await import('./notifications')
          const { adaptNotification } = await import('@/api/notifications')
          const ns = useNotificationsStore()
          const n = adaptNotification(p)
          if (!ns.items.find(x => x.id === n.id)) {
            ns.onReceive(n)
            ElMessage({ message: n.title || '新通知', type: 'info', duration: 4000 })
          }
        } catch {}
      }
      // 设备间同步：单条已读
      hub.handlers.onNotificationRead = async (p) => {
        if (!p?.id) return
        try {
          const { useNotificationsStore } = await import('./notifications')
          const ns = useNotificationsStore()
          const n = ns.items.find(x => x.id === p.id)
          if (n) n.read = true
        } catch {}
      }
      // 设备间同步：一键已读
      hub.handlers.onNotificationsReadAll = () => {
        try {
          import('./notifications').then(({ useNotificationsStore }) => {
            useNotificationsStore().items.forEach(n => (n.read = true))
          })
        } catch {}
      }
      // 好友被删除
      hub.handlers.onFriendRemoved = async (p) => {
        const uid = p?.userId
        if (!uid) return
        try {
          const { useFriendStore } = await import('./friend')
          useFriendStore().removeLocal?.(uid)
        } catch {}
        ElMessage({ message: '你的好友关系已变更', type: 'info' })
      }
      // 会议邀请实时通知
      hub.handlers.onMeetingInvited = (p) => {
        if (!p?.meetingId) return
        ElMessage({
          message: `${p.inviterName || '有人'} 邀请你加入会议`,
          type: 'info',
          duration: 6000,
          onClick: () => {
            import('./meeting').then(({ useMeetingStore }) => {
              useMeetingStore().join(p.meetingId)
            })
          }
        })
      }
      // 会议结束实时通知
      hub.handlers.onMeetingEnded = (p) => {
        if (!p?.meetingId) return
        ElMessage({ message: '会议已结束', type: 'info' })
        // 触发 Messages.vue 关闭房间 UI
        window.dispatchEvent(new CustomEvent('meeting-ended', { detail: p }))
      }
      // IM 4 个新事件
      hub.handlers.onConversationAnnouncementUpdated = (p) => {
        if (!p?.Id && !p?.conversationId) return
        const id = p.Id || p.conversationId
        const c = conversations.value.find(x => x.id === id)
        if (c) {
          c.announcement = p.Announcement ?? p.announcement ?? ''
          c.announcementUpdatedAt = p.AnnouncementUpdatedAt ?? p.announcementUpdatedAt ?? null
        }
      }
      hub.handlers.onConversationRemoved = (p) => {
        if (!p?.ConversationId && !p?.conversationId) return
        const id = p.ConversationId || p.conversationId
        conversations.value = conversations.value.filter(x => x.id !== id)
        if (activeId.value === id) activeId.value = null
      }
      hub.handlers.onConversationDissolved = (p) => {
        if (!p?.ConversationId && !p?.conversationId) return
        const id = p.ConversationId || p.conversationId
        conversations.value = conversations.value.filter(x => x.id !== id)
        if (activeId.value === id) activeId.value = null
      }
      hub.handlers.onMessageReactionUpdated = (p) => {
        if (!p?.id) return
        // 更新该消息的反应数
        for (const cid of Object.keys(messagesMap.value)) {
          const arr = messagesMap.value[cid]
          if (!arr) continue
          const idx = arr.findIndex(m => m.id === p.id)
          if (idx >= 0) {
            arr[idx] = { ...arr[idx], ...p }
            break
          }
        }
      }
    })
    return hub.start()
      .then(() => { connected.value = true })
      .catch(() => {
        connected.value = false
        scheduleHubReconnect()
      })
  }

  function scheduleHubReconnect() {
    if (reconnectTimer || !userStore.isLoggedIn) return
    reconnectTimer = setTimeout(() => {
      reconnectTimer = null
      if (!connected.value && userStore.isLoggedIn) void initHub()
    }, 3000)
  }

  function stopHub(clearSubscriptions = false) {
    if (reconnectTimer) {
      clearTimeout(reconnectTimer)
      reconnectTimer = null
    }
    return hub.stop(clearSubscriptions)
  }

  async function resetSession() {
    await stopHub(true)
    conversations.value = []
    messagesMap.value = {}
    activeId.value = null
    loading.value = false
    connected.value = false
    online.value = true
  }

  async function fetchMessageReads(messageId) {
    const message = Object.values(messagesMap.value)
      .flat()
      .find(item => item.id === messageId)
    if (!message?.conversationId) return { readers: [], readCount: 0 }

    const data = await getConversationReadReceipts(message.conversationId)
    const receipts = Array.isArray(data) ? data : (data?.receipts || data?.items || [])
    const readers = receipts.filter(receipt =>
      receipt.messageId === messageId || receipt.MessageId === messageId
    )
    return { readers, readCount: readers.length }
  }

  /** 创建新会话（私聊场景：优先复用已存在的，避免后端不幂等导致一堆重复会话） */
  async function startConversation(memberUserIds, title) {
    // 私聊且只有 1 个 peer：先在已有 conversations 里查同 peer 的私聊
    if (!title && Array.isArray(memberUserIds) && memberUserIds.length === 1) {
      const peerId = memberUserIds[0]
      // 不能跟自己聊
      if (peerId !== myId()) {
        const exist = conversations.value.find(c => {
          if (c.type !== 'single') return false
          // 必须是 2 人私聊（admin + 这个 peer）
          if (!Array.isArray(c.members) || c.members.length !== 2) return false
          return c.members.some(m => m.userId === peerId) && c.members.some(m => m.userId === myId())
        })
        if (exist) {
          // 复用现有私聊：直接选中
          await selectConversation(exist.id)
          return exist.raw
        }
      }
    }
    const c = await createConversation({ type: 'Private', memberUserIds, title })
    await fetchConversations()
    return c
  }

  return {
    conversations, messagesMap, activeId, loading, connected, online,
    activeConv, activeMessages,
    fetchConversations, selectConversation, loadMessages,
    sendMessage, recallMessage, fetchMessageReads,
    initHub, stopHub, startConversation,
    removeLocalConversation, resetSession,
    appendMessage, handleMessageRead, pruneMessageCache, normalizeMessageCache
  }
}, {
  // 持久化 conversations / messagesMap / activeId 到 localStorage
  // 重启 Electron 后能保留会话列表 + 历史消息
  persist: {
    key: 'feishu-messages',
    storage: localStorage,
    paths: ['conversations', 'messagesMap', 'activeId'],
    afterRestore: ({ store }) => {
      store.normalizeMessageCache?.()
      store.pruneMessageCache?.()
    }
  }
})

function formatHM(d) {
  const pad = (n) => String(n).padStart(2, '0')
  return `${pad(d.getHours())}:${pad(d.getMinutes())}`
}
function formatTime(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  const now = new Date()
  const pad = (n) => String(n).padStart(2, '0')
  if (d.toDateString() === now.toDateString()) return `${pad(d.getHours())}:${pad(d.getMinutes())}`
  const yest = new Date(now); yest.setDate(yest.getDate() - 1)
  if (d.toDateString() === yest.toDateString()) return '昨天'
  return `${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}
