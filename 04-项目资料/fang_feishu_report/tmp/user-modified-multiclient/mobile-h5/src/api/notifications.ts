/**
 * 通知模块 API
 */
import { get, patch } from './request'

/** 通知列表 */
export function getNotifications(params?: {
  page?: number
  pageSize?: number
  type?: string
  unreadOnly?: boolean
}) {
  return get('/notifications', params)
}

/** 未读数 */
export function getUnreadCount() {
  return get('/notifications/unread-count')
}

/** 标记单条已读 */
export function markNotificationRead(id: string) {
  return patch(`/notifications/${id}/read`)
}

/** 全部标已读 */
export function markAllRead() {
  return patch('/notifications/read-all')
}
