import http from './http'

export function listTasks() {
  return http.get('/tasks').then((d) => Array.isArray(d) ? d : (d?.items ?? []))
}
export function getTask(id) {
  return http.get(`/tasks/${id}`)
}
export function createTask(payload) {
  return http.post('/tasks', payload)
}
export function updateTask(id, payload) {
  return http.put(`/tasks/${id}`, payload)
}
export function deleteTask(id) {
  return http.delete(`/tasks/${id}`)
}
export function completeTask(id) {
  return http.patch(`/tasks/${id}/complete`, {})
}
export function reopenTask(id) {
  return http.patch(`/tasks/${id}/reopen`, {})
}
export function setTaskStatus(id, status) {
  return http.patch(`/tasks/${id}/status`, { status })
}

export function adaptTask(t) {
  return {
    id: t.id,
    title: t.title || '未命名任务',
    description: t.description || '',
    status: (t.status || 'Pending').toLowerCase(),
    priority: (t.priority || 'normal').toLowerCase(),
    dueDate: t.dueDate || t.dueAt,
    assigneeId: t.assigneeId,
    assigneeName: t.assigneeName || t.assignee || '',
    creatorId: t.creatorId,
    creatorName: t.creatorName || '',
    createdAt: t.createdAt,
    updatedAt: t.updatedAt,
    completedAt: t.completedAt,
    raw: t
  }
}
