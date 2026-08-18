/**
 * 管理后台 API
 */
import { get, post, put, patch, del } from './request'

// ==================== 用户管理 ====================

export function getUsers(params?: {
  page?: number
  pageSize?: number
  keyword?: string
}) {
  return get('/users', params)
}

export function createUser(data: {
  username: string
  password: string
  realName: string
  email?: string
  phone?: string
  departmentId?: string
  roleCodes?: string[]
}) {
  return post('/users', data)
}

export function updateUser(id: string, data: {
  realName?: string
  email?: string
  phone?: string
  departmentId?: string
  position?: string
  roleCodes?: string[]
}) {
  return put(`/users/${id}`, data)
}

export function setUserStatus(id: string, status: 'Active' | 'Disabled') {
  return patch(`/users/${id}/status`, { status })
}

// ==================== 部门管理 ====================

export function getDepartmentTree() {
  return get('/departments/tree')
}

export function createDepartment(data: {
  name: string
  parentId?: string
  sortOrder?: number
}) {
  return post('/departments', data)
}

export function updateDepartment(id: string, data: {
  name?: string
  parentId?: string
  sortOrder?: number
}) {
  return put(`/departments/${id}`, data)
}

export function deleteDepartment(id: string) {
  return del(`/departments/${id}`)
}

// ==================== 角色管理 ====================

export function getRoles() {
  return get('/roles')
}

export function createRole(data: {
  RoleName: string
  RoleCode: string
  Description?: string
}) {
  return post('/roles', data)
}

// ==================== 操作日志 ====================

export function getOperationLogs(params?: {
  page?: number
  pageSize?: number
  userName?: string
  module?: string
  startDate?: string
  endDate?: string
}) {
  return get('/operation-logs', params)
}
