import http from './http'

/** 用户列表 */
export function listUsers() {
  return http.get('/users')
}

/** 创建用户 */
export function createUser(payload) {
  return http.post('/users', payload)
}

/** 更新用户 */
export function updateUser(id, payload) {
  return http.put(`/users/${id}`, payload)
}

/** 启/禁用 */
export function setUserStatus(id, status) {
  return http.patch(`/users/${id}/status`, { status })
}

// 后端未在 swagger 中给出 status 的确切枚举/大小写，做宽松匹配以兼容多种返回形式
const DISABLED_VALUES = new Set(['disabled', 'inactive', 'blocked', 'false', '0'])

function normalizeStatus(status) {
  if (typeof status === 'boolean') return status ? 'active' : 'disabled'
  if (typeof status === 'number') return status === 0 ? 'disabled' : 'active'
  const s = String(status ?? '').trim().toLowerCase()
  return DISABLED_VALUES.has(s) ? 'disabled' : 'active'
}

/** 适配：后端 user -> 前端 */
export function adaptUser(u) {
  return {
    id: u.id,
    name: u.realName || u.username,
    realName: u.realName,
    username: u.username,
    email: u.email || '',
    phone: u.phone || '',
    dept: u.departmentName || '',
    departmentId: u.departmentId,
    role: (u.roles && u.roles[0]) || 'User',
    roles: u.roles || [],
    position: u.position || '',
    status: normalizeStatus(u.status ?? u.enabled ?? u.isActive),
    lastLogin: u.lastLoginAt ? formatRelative(u.lastLoginAt) : '从未',
    color: pickColor(u.realName || u.username),
    raw: u
  }
}

function formatRelative(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  const diff = (Date.now() - d.getTime()) / 1000
  if (diff < 60) return '刚刚'
  if (diff < 3600) return `${Math.floor(diff / 60)} 分钟前`
  if (diff < 86400) return `${Math.floor(diff / 3600)} 小时前`
  if (diff < 7 * 86400) return `${Math.floor(diff / 86400)} 天前`
  return d.toISOString().slice(0, 10)
}

function pickColor(name) {
  const palette = ['#3370FF', '#FF7A45', '#00B96B', '#9F7AEA', '#EB2F96', '#F59E0B', '#11CDEF', '#5E72E4']
  let h = 0
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) >>> 0
  return palette[h % palette.length]
}