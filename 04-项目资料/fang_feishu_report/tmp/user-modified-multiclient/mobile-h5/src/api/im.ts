/**
 * IM 模块 API
 */
import { get, post, put, del } from './request'

/** 获取会话列表 */
export function getConversations() {
  return get('/im/conversations')
}

/** 获取单个会话详情 */
export function getConversation(id: string) {
  return get(`/im/conversations/${id}`)
}

/** 更新会话（名称、头像等） */
export function updateConversation(id: string, data: any) {
  return put(`/im/conversations/${id}`, data)
}

/** 获取会话消息 */
export function getMessages(conversationId: string, page = 1, pageSize = 20) {
  return get(`/im/conversations/${conversationId}/messages`, { page, pageSize })
}

/** 发送消息（后端接受 messageType + fileId + mentionUserIds） */
export function sendMessage(data: {
  conversationId: string
  content: string
  messageType?: string
  fileId?: string
  mentionUserIds?: string[]
}) {
  return post('/im/messages', data)
}

/** 创建会话（私聊/群聊） */
export function createConversation(data: {
  type: string
  title?: string
  memberUserIds: string[]
}) {
  return post('/im/conversations', data)
}

/** 添加群成员 */
export function addMembers(conversationId: string, userIds: string[]) {
  return post(`/im/conversations/${conversationId}/members`, { userIds })
}

/** 移除群成员（踢人） */
export function removeMember(conversationId: string, userId: string) {
  return del(`/im/conversations/${conversationId}/members/${userId}`)
}

/** 设置管理员 */
export function setAdmins(conversationId: string, adminIds: string[]) {
  return put(`/im/conversations/${conversationId}/admins`, { adminIds })
}

/** 删除/解散会话 */
export function deleteConversation(conversationId: string) {
  return del(`/im/conversations/${conversationId}`)
}

/** 解散群聊（标准接口） */
export function dissolveConversation(conversationId: string) {
  return post(`/im/conversations/${conversationId}/dissolve`)
}

/** 退群 */
export function leaveConversation(conversationId: string) {
  return post(`/im/conversations/${conversationId}/leave`)
}

/** 获取群公告 */
export function getAnnouncement(conversationId: string) {
  return get(`/im/conversations/${conversationId}/announcement`)
}

/** 设置群公告 */
export function updateAnnouncement(conversationId: string, content: string) {
  return put(`/im/conversations/${conversationId}/announcement`, { content })
}

/** 搜索消息 */
export function searchMessages(keyword: string, conversationId?: string) {
  const params: any = { keyword }
  if (conversationId) params.conversationId = conversationId
  return get('/im/messages/search', params)
}

/** 添加表情回复 */
export function addReaction(messageId: string, reactionType: string) {
  return post(`/im/messages/${messageId}/reactions`, { reactionType })
}

/** 删除表情回复 */
export function removeReaction(messageId: string, reactionType: string) {
  return del(`/im/messages/${messageId}/reactions/${encodeURIComponent(reactionType)}`)
}

/** 获取已读回执 */
export function getReadReceipts(conversationId: string) {
  return get(`/im/conversations/${conversationId}/read-receipts`)
}
