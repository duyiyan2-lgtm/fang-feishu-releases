/**
 * 知识库模块 API
 * 后端路由: /api/v1/wiki
 */

import { get, post, put, del } from './request'

// ==================== 空间 ====================

/** 搜索知识库 */
export function searchWiki(keyword: string) {
  return get('/wiki/search', { keyword })
}

/** 获取空间列表 */
export function getWikiSpaces() {
  return get('/wiki/spaces')
}

/** 创建空间 */
export function createWikiSpace(data: {
  name: string
  description?: string
  visibility?: string
}) {
  return post('/wiki/spaces', data)
}

/** 空间详情（含节点列表） */
export function getWikiSpaceDetail(spaceId: string) {
  return get(`/wiki/spaces/${spaceId}`)
}

/** 更新空间 */
export function updateWikiSpace(spaceId: string, data: {
  name: string
  description?: string
  visibility?: string
}) {
  return put(`/wiki/spaces/${spaceId}`, data)
}

/** 删除空间 */
export function deleteWikiSpace(spaceId: string) {
  return del(`/wiki/spaces/${spaceId}`)
}

// ==================== 成员 ====================

/** 获取空间成员 */
export function getWikiSpaceMembers(spaceId: string) {
  return get(`/wiki/spaces/${spaceId}/members`)
}

/** 设置空间成员 */
export function setWikiSpaceMembers(spaceId: string, userIds: string[], permission: string) {
  return put(`/wiki/spaces/${spaceId}/members`, { userIds, permission })
}

// ==================== 节点 ====================

/** 获取空间节点 */
export function getWikiNodes(spaceId: string) {
  return get(`/wiki/spaces/${spaceId}/nodes`)
}

/** 创建节点 */
export function createWikiNode(spaceId: string, data: {
  parentId?: string
  documentId?: string
  title: string
  sortOrder?: number
}) {
  return post(`/wiki/spaces/${spaceId}/nodes`, data)
}

/** 更新节点 */
export function updateWikiNode(spaceId: string, nodeId: string, data: {
  parentId?: string
  documentId?: string
  title: string
  sortOrder?: number
}) {
  return put(`/wiki/spaces/${spaceId}/nodes/${nodeId}`, data)
}

/** 删除节点 */
export function deleteWikiNode(spaceId: string, nodeId: string) {
  return del(`/wiki/spaces/${spaceId}/nodes/${nodeId}`)
}
