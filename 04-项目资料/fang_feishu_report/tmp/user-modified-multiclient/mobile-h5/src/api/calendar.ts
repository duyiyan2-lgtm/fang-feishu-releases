/**
 * 日历模块 API
 */

import { get, post, put, del, patch } from './request'

/** 日程列表（后端参数名：from / to） */
export function getEvents(params?: { from?: string; to?: string }) {
  return get('/calendar/events', params)
}

/** 新增日程（支持参会人 + 重复） */
export function createEvent(data: {
  title: string
  startTime: string
  endTime: string
  location?: string
  description?: string
  attendeeUserIds?: string[]
  recurrenceType?: string
  recurrenceUntil?: string
}) {
  return post('/calendar/events', data)
}

/** 编辑日程（支持参会人 + 重复） */
export function updateEvent(id: string, data: {
  title?: string
  startTime?: string
  endTime?: string
  location?: string
  description?: string
  attendeeUserIds?: string[]
  recurrenceType?: string
  recurrenceUntil?: string
}) {
  return put(`/calendar/events/${id}`, data)
}

/** 更新出席状态 */
export function updateAttendance(eventId: string, status: 'Accepted' | 'Declined' | 'Tentative') {
  return patch(`/calendar/events/${eventId}/attendance`, { status })
}

/** 删除日程 */
export function deleteEvent(id: string) {
  return del(`/calendar/events/${id}`)
}

/** 获取重复日程的所有发生日期 */
export function getOccurrences(id: string) {
  return get(`/calendar/events/${id}/occurrences`)
}

/** 查询空闲时间 */
export function getFreeBusy(params: { from: string; to: string; userIds?: string[] }) {
  // 手动构建查询字符串，确保 userIds 以重复参数格式传递（?userIds=a&userIds=b）
  let url = `/calendar/events/free-busy?from=${encodeURIComponent(params.from)}&to=${encodeURIComponent(params.to)}`
  if (params.userIds?.length) {
    url += '&' + params.userIds.map(id => `userIds=${encodeURIComponent(id)}`).join('&')
  }
  return get(url)
}
