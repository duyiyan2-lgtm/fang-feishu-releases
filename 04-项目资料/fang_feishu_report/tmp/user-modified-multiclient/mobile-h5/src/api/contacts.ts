/**
 * 通讯录模块 API（好友系统）
 */

import { get, post, patch, del } from './request'

/** 获取好友列表 */
export function getFriends() {
  return get('/contacts')
}

/** 发现用户（搜索非好友） */
export function discoverUsers(keyword?: string) {
  return get('/contacts/discover', keyword ? { keyword } : undefined)
}

/** 获取好友申请列表 */
export function getFriendRequests() {
  return get('/contacts/requests')
}

/** 发送好友申请 */
export function sendFriendRequest(userId: string, greeting?: string) {
  return post('/contacts/requests', { userId, greeting })
}

/** 接受好友申请 */
export function acceptFriendRequest(id: string) {
  return patch(`/contacts/requests/${id}/accept`)
}

/** 拒绝好友申请 */
export function rejectFriendRequest(id: string) {
  return patch(`/contacts/requests/${id}/reject`)
}

/** 删除好友 */
export function removeFriend(userId: string) {
  return del(`/contacts/friends/${userId}`)
}
