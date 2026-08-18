/**
 * 任务管理模块 API
 * 后端路由: /api/v1/tasks
 */

import { get, post, put, patch, del } from './request'

/** 任务列表 */
export function getTasks(params?: {
  scope?: 'all' | 'assigned' | 'created'
  status?: 'Todo' | 'InProgress' | 'Completed'
}) {
  return get('/tasks', params)
}

/** 创建任务 */
export function createTask(data: {
  title: string
  description?: string
  assigneeId?: string
  dueAt?: string
}) {
  return post('/tasks', data)
}

/** 任务详情 */
export function getTaskDetail(id: string) {
  return get(`/tasks/${id}`)
}

/** 更新任务 */
export function updateTask(id: string, data: {
  title?: string
  description?: string
  assigneeId?: string
  dueAt?: string
}) {
  return put(`/tasks/${id}`, data)
}

/** 更新任务状态 */
export function updateTaskStatus(id: string, status: 'Todo' | 'InProgress' | 'Completed') {
  return patch(`/tasks/${id}/status`, { status })
}

/** 完成任务 */
export function completeTask(id: string) {
  return patch(`/tasks/${id}/complete`)
}

/** 重新打开任务 */
export function reopenTask(id: string) {
  return patch(`/tasks/${id}/reopen`)
}

/** 删除任务 */
export function deleteTask(id: string) {
  return del(`/tasks/${id}`)
}
