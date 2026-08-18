// 加好友 API 封装（对齐后端 v1.1(2)）
import http from './http'

/** 好友列表（已 accepted）— 实际就是 GET /contacts */
export function listFriends() {
  return http.get('/contacts')
}

/** 探索/搜索用户（带 keyword 是搜索，不带是推荐） */
export function discoverUsers(keyword) {
  return http.get('/contacts/discover', { params: keyword ? { keyword } : {} })
}

/** 列出我相关的待处理请求（含我发的 Outgoing 和我收的 Incoming） */
export function listRequests() {
  return http.get('/contacts/requests')
}

/** 发起加好友请求（后端用 userId 字段） */
export function sendFriendRequest(userId, greeting) {
  return http.post('/contacts/requests', { userId, greeting })
}

/** 接受好友请求（PATCH） */
export function acceptFriendRequest(requestId) {
  return http.patch(`/contacts/requests/${requestId}/accept`)
}

/** 拒绝好友请求（PATCH） */
export function rejectFriendRequest(requestId) {
  return http.patch(`/contacts/requests/${requestId}/reject`)
}

/** 删除好友（双向） */
export function removeFriend(userId) {
  return http.delete(`/contacts/friends/${userId}`)
}