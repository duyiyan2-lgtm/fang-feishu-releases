/**
 * 审批模块 API
 */

import { get, post, patch } from './request'

/** 审批列表 */
export function getApprovals(params?: {
  page?: number
  pageSize?: number
  status?: string
  search?: string
}) {
  return get('/approvals', params)
}

/** 提交请假 */
export function createApproval(data: {
  type: string
  startTime: string
  endTime: string
  content: string
}) {
  return post('/approvals', data)
}

/** 通过审批 */
export function approveApproval(id: string, data?: { comment?: string }) {
  return patch(`/approvals/${id}/approve`, data)
}

/** 驳回审批 */
export function rejectApproval(id: string, data?: { comment?: string }) {
  return patch(`/approvals/${id}/reject`, data)
}

/** 撤回申请（仅申请人，待审批状态） */
export function withdrawApproval(id: string) {
  return post(`/approvals/${id}/withdraw`)
}

/** 提醒审批人（仅申请人，待审批状态） */
export function remindApproval(id: string) {
  return post(`/approvals/${id}/remind`)
}

/** 审批模板列表 */
export function getTemplates() {
  return get('/approvals/templates')
}
