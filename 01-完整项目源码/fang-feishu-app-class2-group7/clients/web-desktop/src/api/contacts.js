import http from './http'

/** 通讯录列表（按 departmentId 或 keyword 筛） */
export function listContacts(params = {}) {
  return http.get('/contacts', { params })
}

/** 通讯录搜索（不传 keyword 时返所有用户） */
export function searchContacts(keyword) {
  // 后端 [FromQuery] string keyword 必填，空字符串会 400
  // 传 null 让 axios 不带这个参数
  return http.get('/contacts/search', keyword ? { params: { keyword } } : {})
}

/** 单个联系人详情 */
export function getContact(id) {
  return http.get(`/contacts/${id}`)
}

/** 部门树（嵌套结构） */
export function getDepartmentTree() {
  return http.get('/departments/tree')
}

/** 新增/更新/删除部门（管理后台） */
export function createDepartment(payload) {
  return http.post('/departments', payload)
}
export function updateDepartment(id, payload) {
  return http.put(`/departments/${id}`, payload)
}
export function deleteDepartment(id) {
  return http.delete(`/departments/${id}`)
}

/**
 * 适配后端 Contact -> 前端组件使用
 */
export function adaptContact(c) {
  return {
    id: c.id,
    name: c.realName || c.username || '',
    realName: c.realName,
    username: c.username,
    title: c.position || '',
    dept: c.departmentName || '',
    departmentId: c.departmentId,
    color: pickColor(c.realName || c.username || ''),
    online: c.status === 'active' || c.online === true,
    phone: c.phone || '',
    email: c.email || '',
    avatarUrl: c.avatarUrl || null,
    workPlace: c.workPlace || '',
    bio: c.bio || ''
  }
}

// 按姓名取一个稳定的颜色（hash）
function pickColor(name) {
  const palette = ['#3370FF', '#FF7A45', '#00B96B', '#9F7AEA', '#EB2F96', '#F59E0B', '#11CDEF', '#5E72E4']
  let h = 0
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) >>> 0
  return palette[h % palette.length]
}
