import http from './http'

/**
 * 会话列表
 */
export function listConversations() {
  return http.get('/im/conversations')
}

/**
 * 创建会话（单聊：传一个 userId；群聊：传多个）
 */
export function createConversation(payload) {
  return http.post('/im/conversations', payload)
}

/**
 * 拉历史消息
 */
export function listMessages(conversationId, page = 1, pageSize = 30) {
  return http.get(`/im/conversations/${conversationId}/messages`, { params: { page, pageSize } })
}

/**
 * 发消息（HTTP 兜底，实时优先走 SignalR）
 */
export function sendMessageHttp(payload) {
  return http.post('/im/messages', payload)
}

/**
 * 撤回消息
 */
export function recallMessageHttp(messageId) {
  return http.patch(`/im/messages/${messageId}/recall`)
}

/**
 * 标记会话已读
 */
export function markConversationRead(conversationId) {
  return http.patch(`/im/conversations/${conversationId}/read`)
}

/**
 * 获取群详情
 */
export function getGroupDetail(conversationId) {
  return http.get(`/im/groups/${conversationId}`)
}

/**
 * 加成员（向后端真实端点 POST /im/conversations/{id}/members）
 * @param {string[]} memberUserIds
 */
export function addMembers(conversationId, memberUserIds) {
  return http.post(`/im/conversations/${conversationId}/members`, { memberUserIds })
}

/**
 * 设管理员（PUT /im/conversations/{id}/admins）
 * @param {string[]} adminIds
 */
export function setAdmins(conversationId, adminIds) {
  return http.put(`/im/conversations/${conversationId}/admins`, { adminIds })
}

/**
 * 修改群资料（PUT /im/conversations/{id}）
 * @param {{title?: string, avatar?: string}} payload
 */
export function updateConversation(conversationId, payload) {
  return http.put(`/im/conversations/${conversationId}`, payload)
}

/**
 * 移除成员（DELETE /im/conversations/{id}/members/{userId}）
 */
export function removeMember(conversationId, userId) {
  return http.delete(`/im/conversations/${conversationId}/members/${userId}`)
}

/**
 * 退群（POST /im/conversations/{id}/leave）
 */
export function leaveGroup(conversationId) {
  return http.post(`/im/conversations/${conversationId}/leave`, {})
}

/**
 * 解散群（POST /im/conversations/{id}/dissolve）
 */
export function dissolveGroup(conversationId) {
  return http.post(`/im/conversations/${conversationId}/dissolve`, {})
}

/**
 * 改群公告（PUT /im/conversations/{id}/announcement）
 * @param {string} content
 */
export function updateAnnouncement(conversationId, content) {
  return http.put(`/im/conversations/${conversationId}/announcement`, { content })
}

/** 拉群公告 GET（{ announcement, announcementUpdatedAt }） */
export function getAnnouncement(conversationId) {
  return http.get(`/im/conversations/${conversationId}/announcement`)
}

/** 全局消息搜索 GET /im/messages/search?keyword= */
export function searchMessages(keyword) {
  return http.get('/im/messages/search', { params: { keyword } }).then((d) => {
    if (Array.isArray(d)) return d
    if (Array.isArray(d?.items)) return d.items
    return []
  })
}

/** 删会话 */
export function deleteConversation(id) {
  return http.delete(`/im/conversations/${id}`)
}

/**
 * 拉群详情（GET /im/conversations/{id}）
 */
export function getGroupDetailById(conversationId) {
  return http.get(`/im/conversations/${conversationId}`)
}

/**
 * 已读回执（GET /im/conversations/{id}/read-receipts）
 */
export function getConversationReadReceipts(conversationId) {
  return http.get(`/im/conversations/${conversationId}/read-receipts`)
}

/**
 * 适配：会话 -> 前端使用的形状
 */
export function adaptConversation(c, currentUserId) {
  // 对端名字（单聊时取非自己那位；群组用 title）
  let peerName = c.title
  let peerId = null
  if (c.type === 'Private' && Array.isArray(c.members)) {
    const peer = c.members.find(m => m.userId !== currentUserId)
    if (peer) { peerName = peer.realName || peer.username; peerId = peer.userId }
  } else if (c.type === 'Group' && (!peerName || !String(peerName).trim())) {
    // 群组 title 为空时，取前 3 个成员名字拼接作为兜底
    const others = (c.members || []).filter(m => m.userId !== currentUserId)
    peerName = others.slice(0, 3).map(m => m.realName || m.username).join('、') || '群聊'
  }
  // 兜底：保证 peerName 始终是非空字符串
  peerName = peerName || (c.type === 'Group' ? '群聊' : '未知会话')
  return {
    id: c.id,
    type: c.type === 'Group' ? 'group' : 'single',
    title: c.title || peerName,
    name: peerName,
    members: c.members || [],
    lastMessage: c.lastMessage?.content || '',
    lastSender: c.lastMessage?.senderName || '',
    lastTime: formatTime(c.lastMessage?.createdAt || c.createdAt),
    lastIsRecalled: !!c.lastMessage?.isRecalled,
    unread: c.unreadCount || 0,
    raw: c
  }
}

export function adaptMessage(m, currentUserId) {
  return {
    id: m.id,
    conversationId: m.conversationId,
    senderId: m.senderId,
    senderName: m.senderName,
    content: m.content || '',
    type: m.messageType || 'Text',
    fileId: m.fileId || null,
    fileName: m.fileName || null,
    recalled: !!m.isRecalled,
    time: formatTime(m.createdAt),
    // 直接用 senderId === currentUserId 判断，currentUserId 为 undefined 时一律 'other'
    sender: (currentUserId && m.senderId === currentUserId) ? 'me' : 'other',
    raw: m
  }
}

function formatTime(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  const now = new Date()
  const pad = (n) => String(n).padStart(2, '0')
  if (d.toDateString() === now.toDateString()) {
    return `${pad(d.getHours())}:${pad(d.getMinutes())}`
  }
  const yest = new Date(now); yest.setDate(yest.getDate() - 1)
  if (d.toDateString() === yest.toDateString()) return '昨天'
  return `${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}