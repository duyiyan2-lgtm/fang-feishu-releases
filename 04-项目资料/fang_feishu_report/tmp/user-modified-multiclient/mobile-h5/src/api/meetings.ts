/**
 * 视频会议模块 API
 * 后端路由: /api/v1/meetings
 */

import { get, post, patch, del } from './request'

/** 会议列表 */
export function getMeetings(params?: { status?: string }) {
  return get('/meetings', params)
}

/** 创建会议 */
export function createMeeting(data: {
  title?: string
  roomName?: string
  roomId?: string
  memberUserIds?: string[]
  scheduledStartAt?: string
  scheduledEndAt?: string
}) {
  return post('/meetings', data)
}

/** 会议详情 */
export function getMeetingDetail(id: string) {
  return get(`/meetings/${id}`)
}

/** 加入会议（返回 Agora 凭证） */
export function joinMeeting(id: string) {
  return post(`/meetings/${id}/join`, { autoCamera: true, autoMic: true })
}

/** 离开会议 */
export function leaveMeeting(id: string) {
  return post(`/meetings/${id}/leave`)
}

/** 邀请成员 */
export function inviteMeetingMembers(id: string, memberUserIds: string[]) {
  return post(`/meetings/${id}/invite`, { memberUserIds })
}

/** 结束会议 */
export function endMeeting(id: string) {
  return patch(`/meetings/${id}/end`)
}

/** 更新会议日程 */
export function updateMeetingSchedule(id: string, data: {
  scheduledStartAt?: string
  scheduledEndAt?: string
}) {
  return patch(`/meetings/${id}/schedule`, data)
}

/** 会议聊天消息列表 */
export function getMeetingChatMessages(id: string, params?: { page?: number; pageSize?: number }) {
  return get(`/meetings/${id}/chat`, params)
}

/** 发送会议聊天消息 */
export function sendMeetingChatMessage(id: string, content: string) {
  return post(`/meetings/${id}/chat`, { content })
}

/** 会议统计 */
export function getMeetingStatistics(id: string) {
  return get(`/meetings/${id}/statistics`)
}
