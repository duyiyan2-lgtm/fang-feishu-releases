// 会议 API 封装
import http from './http'

/** 列会议（可按状态过滤） */
export function listMeetings(status) {
  return http.get('/meetings', { params: status ? { status } : {} })
}

/** 创建会议 */
export function createMeeting(payload) {
  return http.post('/meetings', payload)
}

/** 会议详情 */
export function getMeetingDetail(id) {
  return http.get(`/meetings/${id}`)
}

/** 加入会议（返回 Agora token） */
export function joinMeeting(id) {
  return http.post(`/meetings/${id}/join`, {})
}

/** 离开会议 */
export function leaveMeetingApi(id) {
  return http.post(`/meetings/${id}/leave`, {})
}

/** 邀请成员 */
export function inviteMeetingMembers(id, memberUserIds) {
  return http.post(`/meetings/${id}/invite`, { memberUserIds })
}

/** 结束会议 */
export function endMeeting(id) {
  return http.patch(`/meetings/${id}/end`, {})
}

/** 排期 */
export function scheduleMeeting(id, payload) {
  return http.patch(`/meetings/${id}/schedule`, payload)
}

/** 会议聊天记录 */
export function getMeetingChat(id) {
  return http.get(`/meetings/${id}/chat`)
}

/** 发送会议聊天 */
export function postMeetingChat(id, content) {
  return http.post(`/meetings/${id}/chat`, { content })
}

/** 会议统计 */
export function getMeetingStatistics(id) {
  return http.get(`/meetings/${id}/statistics`)
}