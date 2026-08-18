/**
 * 文档模块 API
 */

import { get, post, put, patch, del } from './request'

/** 文档列表 */
export function getDocuments(params?: { page?: number; pageSize?: number; search?: string }) {
  return get('/documents', params)
}

/** 新建文档 */
export function createDocument(data: { title: string; content?: string }) {
  return post('/documents', data)
}

/** 文档详情 */
export function getDocument(id: string) {
  return get(`/documents/${id}`)
}

/** 保存文档 */
export function updateDocument(id: string, data: { title?: string; content?: string }) {
  return put(`/documents/${id}`, data)
}

/** 发表评论 */
export function addComment(id: string, data: { content: string }) {
  return post(`/documents/${id}/comments`, data)
}

/** 版本记录 */
export function getVersions(id: string) {
  return get(`/documents/${id}/versions`)
}

/** 删除文档 */
export function deleteDocument(id: string) {
  return del(`/documents/${id}`)
}

/** 获取协作者列表 */
export function getCollaborators(id: string) {
  return get(`/documents/${id}/collaborators`)
}

/** 设置协作者（permission: View=只读, Edit=可编辑） */
export function updateCollaborators(id: string, userIds: string[], permission: string = 'Edit') {
  return put(`/documents/${id}/collaborators`, { userIds, permission })
}

/** 更新可见性（Organization=全员可见, Private=仅自己） */
export function updateVisibility(id: string, visibility: 'Organization' | 'Private') {
  return patch(`/documents/${id}/visibility`, { visibility })
}

/** 恢复历史版本 */
export function restoreVersion(id: string, versionId: string) {
  return post(`/documents/${id}/versions/${versionId}/restore`)
}

/** 删除评论 */
export function deleteComment(id: string, commentId: string) {
  return del(`/documents/${id}/comments/${commentId}`)
}
