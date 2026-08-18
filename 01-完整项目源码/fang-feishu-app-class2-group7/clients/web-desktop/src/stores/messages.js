import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import {
  listConversations, listMessages, sendMessageHttp, recallMessageHttp, markConversationRead,
  createConversation, adaptConversation, adaptMessage, getConversationReadReceipts
} from '@/api/im'
import { ElMessage } from '@/api/toast'
import hub from '@/utils/signalr'
import { useUserStore } from '@/stores/user'

export const useMessagesStore = defineStore('messages', () => {
  const userStore = useUserStore()
  const conversations = ref([]) // adaptConversation 后的列表
  const messagesMap = ref({})   // { [conversationId]: Message[] }
  const activeId = ref(null)
  const loading = ref(false)
  const connected = ref(false)
  const online = ref(true) // 兼容旧字段

  const activeConv = computed(() => conversations.value.find((c) => c.id === activeId.value))
  const activeMessages = computed(() => messagesMap.value[activeId.value] || [])

  // 启动时去重：清理持久化里可能存在的旧重复消息
  // 按 (id + content + sender) 三重去重，相同 id、内容、发送者只保留第一条
  for (const convId of Object.keys(messagesMap.value)) {
    const arr = messagesMap.value[convId] || []
    const seenId = new Set()
    const seenContent = new Set()
    messagesMap.value[convId] = arr.filter(m => {
      // 乐观消息（tmp-）保留
      if (m.id && m.id.startsWith('tmp-')) return true
      // 真实消息：先按 id 去重
      if (seenId.has(m.id)) return false
      seenId.add(m.id)
      // 再按 content+sender 去重（处理历史重复）
      const key = `${m.sender || 'me'}|${m.content || ''}`
      if (seenContent.has(key)) return false
      seenContent.add(key)
      return true
    })
  }

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
        if (old) return { ...n, unread: old.unread || n.unread, // 保留未读
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
    // 如果 messagesMap[id] 不存在 或 空数组 → 从后端拉历史
    if (!messagesMap.value[id] || messagesMap.value[id].length === 0) {
      await loadMessages(id)
    }
    // 加入 SignalR 群组
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
      messagesMap.value[conversationId] = items.map(m => adaptMessage(m, myId())).reverse()
    } catch (e) {
      console.error('[messages] loadMessages', e)
      messagesMap.value[conversationId] = []
    }
  }

  /** 发送消息 */
  async function sendMessage(content) {
    const text = (content || '').trim()
    if (!text || !activeId.value) return
    const convId = activeId.value
    try {
      // 优先 SignalR，失败再 HTTP 兜底
      try {
        await hub.sendMessage(convId, text, 'Text')
        // SignalR 模式下 ReceiveMessage 会回调，下面乐观更新让 UI 立即响应
      } catch {
        const msg = await sendMessageHttp({ conversationId: convId, content: text, messageType: 'Text' })
        const m = adaptMessage(msg, myId())
        appendMessage(convId, m)
      }
      // 乐观更新（确保即时反馈）
      const optimistic = {
        id: 'tmp-' + Date.now(),
        conversationId: convId,
        content: text,
        sender: 'me',
        senderName: userStore.userInfo?.realName || '我',
        time: formatHM(new Date()),
        recalled: false,
        pending: true
      }
      appendMessage(convId, optimistic)
      // 更新会话预览
      updateConvPreview(convId, text, userStore.userInfo?.realName || '我', new Date().toISOString())
    } catch (e) {
      ElMessage({ message: '发送失败：' + (e.message || '未知错误'), type: 'error' })
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
      x.pending && x.content === m.content &&
      (x.senderId === m.senderId || x.senderName === m.senderName)
    )
    if (idx > -1) {
      m._ts = Date.now()
      m.readers = m.readers || []
      m.readCount = m.readers.length || 0
      messagesMap.value[convId].splice(idx, 1, m)
      return
    }

    // 去重 2：按 content + senderName + 30 秒时间窗（防重复触发）
    if (m.content && m.senderName) {
      const now = Date.now()
      const dup = messagesMap.value[convId].find(x =>
        x.content === m.content &&
        (x.senderName || x.sender) === m.senderName &&
        Math.abs(now - (x._ts || 0)) < 30000
      )
      if (dup) return
    }
    m._ts = Date.now()
    // 新增：已读回执字段
    m.readers = m.readers || []
    m.readCount = m.readers.length || 0

    // 否则 push
    messagesMap.value[convId].push(m)
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
    // 好友请求实时通知（动态 import 避免循环依赖）
    import('./friend').then(({ useFriendStore }) => {
      const fs = useFriendStore()
      hub.handlers.onFriendRequestReceived = (p) => {
        if (p?.id) {
          fs.pendingReceived.value.push({
            id: p.id,
            requester: { id: p.requesterId, realName: p.requesterName },
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
    hub.start()
    // 监听连接状态：hub 暴露 whenConnected() Promise，能可靠捕获「首次连接成功」和「重连成功」
    if (hub.connection) {
      hub.connection.onclose(() => { connected.value = false })
    }
    hub.whenConnected()
      .then(() => { connected.value = true })
      .catch(() => { connected.value = false })
  }

  function stopHub() {
    hub.stop()
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
    removeLocalConversation,
    appendMessage, handleMessageRead
  }
}, {
  // 持久化 conversations / messagesMap / activeId 到 localStorage
  // 重启 Electron 后能保留会话列表 + 历史消息
  persist: {
    key: 'feishu-messages',
    storage: localStorage,
    paths: ['conversations', 'messagesMap', 'activeId']
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